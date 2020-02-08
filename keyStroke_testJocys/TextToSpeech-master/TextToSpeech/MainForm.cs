using JocysCom.ClassLibrary.Runtime;
using JocysCom.TextToSpeech.Monitor.Audio;
using JocysCom.TextToSpeech.Monitor.Controls;
using JocysCom.TextToSpeech.Monitor.Network;
using SpeechLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace JocysCom.TextToSpeech.Monitor
{
	public partial class MainForm : Form
	{
        public string NVDA_MSG = "";
        public Boolean notFirst = false;
        public int tester = 0;

		public MainForm()
		{
			Program.TopForm = this;
			InitializeComponent();
			WavPlayer = new System.Media.SoundPlayer();
			playlist = new BindingList<PlayItem>();
			playlist.ListChanged += playlist_ListChanged;
			PlayListDataGridView.AutoGenerateColumns = false;
			PlayListDataGridView.DataSource = playlist;
			Text = MainHelper.GetProductFullName();
			InstalledVoices = new BindingList<InstalledVoiceEx>();
			DefaultIntroSoundComboBox.DataSource = GetIntroSoundNames();
			UpdateLabel.Text = "You are running " + MainHelper.GetProductFullName();
		}

		System.Media.SoundPlayer WavPlayer;

		void InstalledVoices_ListChanged(object sender, ListChangedEventArgs e)
		{

			ValidateList();
		}

		void ValidateList()
		{
			string error = "";
			if (InstalledVoices.Count == 0) error = "No voices were found";
			else if (!InstalledVoices.Any(x => x.Female > 0)) error = "Please set popularity value higher than 0 for at least one voice in \"Female\" column to use it as female voice ( recommended value: 100 ).";
			else if (!InstalledVoices.Any(x => x.Female > 0 && x.Enabled)) error = "Please enable and set popularity value higher than 0 ( recommended value: 100 ) in \"Female\" column for at least one voice to use it as female voice.";
			else if (!InstalledVoices.Any(x => x.Male > 0)) error = "Please set popularity value higher than 0 for at least one voice in \"Male\" column to use it as male voice ( recommended value: 100 ).";
			else if (!InstalledVoices.Any(x => x.Male > 0 && x.Enabled)) error = "Please enable and set popularity value higher than 0 ( recommended value: 100 ) in \"Male\" column for at least one voice to use it as male voice.";
			else if (!InstalledVoices.Any(x => x.Neutral > 0)) error = "Please set popularity value higher than 0 for at least one voice in \"Neutral\" column to use it as neutral voice ( recommended value: 100 ).";
			else if (!InstalledVoices.Any(x => x.Neutral > 0 && x.Enabled)) error = "Please enable and set popularity value higher than 0 ( recommended value: 100 ) in \"Neutral\" column for at least one voice to use it as neutral voice.";
			VoiceErrorLabel.Visible = error.Length > 0;
			VoiceErrorLabel.Text = error;
		}

		/// <summary>
		/// This event will fire when item is added, removed or change in the list.
		/// </summary>
		void playlist_ListChanged(object sender, ListChangedEventArgs e)
		{
			var changed = (e.ListChangedType == ListChangedType.ItemChanged && e.PropertyDescriptor != null && e.PropertyDescriptor.Name == "Status");
			var added = e.ListChangedType == ListChangedType.ItemAdded;
			var deleted = e.ListChangedType == ListChangedType.ItemDeleted;
			var reset = e.ListChangedType == ListChangedType.Reset;
			if (added || deleted || reset)
			{
				BeginInvoke((Action)(() =>
				{
					var items = playlist.ToArray();
					PlayListTabPage.Text = items.Length == 0
						? "Play List"
						: string.Format("Play List: {0}", items.Length);
				}));
			}
			CheckPlayList(added, changed);
		}

		void CheckPlayList(bool added, bool changed)
		{
			// If new item was added or item status changed then...
			var items = playlist.ToArray();
			if (added || changed)
			{
				// If nothing is playing then...
				if (!items.Any(x => x.Status == JobStatusType.Playing))
				{
					// Get first item ready to play.
					var pitchedItem = items.FirstOrDefault(x => x.Status == JobStatusType.Pitched);
					//playlist.Remove(item);
					if (pitchedItem != null)
					{
						if (pitchedItem.StreamData != null)
						{
							var fs = pitchedItem.StreamData;
							BinaryReader br = new BinaryReader(fs);
							var length = (int)fs.Length - 8;
							fs.Position = 22;
							var channelCount = br.ReadInt16();
							fs.Position = 24;
							var sampleRate = br.ReadInt32();
							fs.Position = 34;
							var bitsPerSample = br.ReadInt16();
							var dataLength = (int)fs.Length - 44;
							var duration = ((decimal)dataLength * 8m) / (decimal)channelCount / (decimal)sampleRate / (decimal)bitsPerSample * 1000m;
							pitchedItem.Duration = (int)duration;
							// Play.
							fs.Position = 0;
							WavPlayer.Stream = fs;
							WavPlayer.Play();
							// Start timer which will reset status to Played
							pitchedItem.StartPlayTimer();
						}
						else
						{
							// Must be outside begin invoke.
							var sampleRate = (int)AudioSampleRateComboBox.SelectedItem;
							var bitsPerSample = (int)AudioBitsPerSampleComboBox.SelectedItem;
							var channelCount = (int)(AudioChannel)AudioChannelsComboBox.SelectedItem;
							EffectPresetsEditorSoundEffectsControl.LoadSoundFile(pitchedItem.WavData, sampleRate, bitsPerSample, channelCount);
							EffectPresetsEditorSoundEffectsControl.PlaySound();
							// Start timer which will reset status to Played
							pitchedItem.StartPlayTimer();
						}
					}
				}
				// If last item finished playing or any item resulted in error then clear then..
				var lastItem = items.LastOrDefault();
				if ((lastItem != null && lastItem.Status == JobStatusType.Played) || (items.Any(x => x.Status == JobStatusType.Error)))
				{
					BeginInvoke((Action)(() =>
					{
						bool groupIsPlaying;
						int itemsLeftToPlay;
						lock (playlistLock) { ClearPlayList(null, out groupIsPlaying, out itemsLeftToPlay); }
					}));
				}
				else
				{
					BeginInvoke((Action)(() =>
					{
						lock (threadIsRunningLock)
						{
							// If thread is not running or stopped then...
							if (!threadIsRunning)
							{
								threadIsRunning = true;
								ThreadPool.QueueUserWorkItem(ProcessPlayItems);
							}
						}
					}));
				}
			}
		}

		/// <summary>
		/// If group specified then remove only items from the group.
		/// </summary>
		/// <param name="group"></param>
		void ClearPlayList(string group, out bool groupIsPlaying, out int itemsLeftToPlay)
		{
			PlayItem[] itemsToClear;
			if (string.IsNullOrEmpty(group))
			{
				itemsToClear = playlist.ToArray();
				groupIsPlaying = false;
			}
			else
			{
				itemsToClear = playlist.Where(x => x.Group != null && x.Group.ToLower() == group.ToLower()).ToArray();
				groupIsPlaying = itemsToClear.Any(x => x.Status == JobStatusType.Playing);
			}
			foreach (var item in itemsToClear)
			{
				item.Dispose();
				playlist.Remove(item);
			}
			itemsLeftToPlay = playlist.Count();
		}

		bool threadIsRunning;
		object threadIsRunningLock = new object();

		/// <summary>
		/// Thread which will process all play items and convert XML to WAV bytes.
		/// </summary>
		/// <param name="status"></param>
		void ProcessPlayItems(object status)
		{

			while (true)
			{
				PlayItem item = null;
				lock (threadIsRunningLock)
				{
					lock (playlistLock)
					{
						// Get first incomplete item in the list.
						JobStatusType[] validStates = { JobStatusType.Parsed, JobStatusType.Synthesized };
						item = playlist.FirstOrDefault(x => validStates.Contains(x.Status));
						// If nothing to do then...
						if (item == null || playlist.Any(x => x.Status == JobStatusType.Error))
						{
							// Exit thread.
							threadIsRunning = false;
							return;
						}
					}
				}
				try
				{
					// If XML is available.
					if (item.Status == JobStatusType.Parsed)
					{
						item.Status = JobStatusType.Synthesizing;
						item.WavData = ConvertSapiXmlToWav(item.Xml);
						item.Status = item.WavData == null
							? item.Status = JobStatusType.Error
							: item.Status = JobStatusType.Synthesized;
					}
					if (item.Status == JobStatusType.Synthesized)
					{
						item.Status = JobStatusType.Pitching;
						ApplyPitch(item);
						item.Status = JobStatusType.Pitched;
					}
				}
				catch (Exception ex)
				{
					LastException = ex;
					item.Status = JobStatusType.Error;
					// Exit thread.
					threadIsRunning = false;
					return;
				}
			}
		}

		void ApplyPitch(PlayItem item)
		{
			var ms = new MemoryStream();
			var writer = new System.IO.BinaryWriter(ms);
			int channelCount = 0;
			int sampleRate = 0;
			int bitsPerSample = 0;
			bool applyEffects = false;
			float pitchShift = 1.0F;
			Invoke((Action)(() =>
			{
				channelCount = (int)(AudioChannel)AudioChannelsComboBox.SelectedItem;
				sampleRate = (int)AudioSampleRateComboBox.SelectedItem;
				bitsPerSample = (int)AudioBitsPerSampleComboBox.SelectedItem;
				applyEffects = EffectPresetsEditorSoundEffectsControl.GeneralCheckBox.Checked;
				pitchShift = ((float)EffectPresetsEditorSoundEffectsControl.GeneralPitchTrackBar.Value / 100F);

			}));
			var bytes = item.WavData;
			// Add 100 milliseconds at the start.
			var silenceStart = 100;
			// Add 200 milliseconds at the end.
			var silenceEnd = 200;
			var silenceBytes = AudioHelper.GetSilenceByteCount(channelCount, sampleRate, bitsPerSample, silenceStart + silenceEnd);
			// Comment WriteHeader(...) line, because SharpDX don't need that (it creates noise).
			//AudioHelper.WriteHeader(writer, bytes.Length + silenceBytes, channelCount, sampleRate, bitsPerSample);
			if (applyEffects)
			{
				token = new CancellationTokenSource();
				// This part could take long time.
				bytes = EffectsGeneral.ApplyPitchShift(bytes, channelCount, sampleRate, bitsPerSample, pitchShift, token);
				// If pitch shift was cancelled then...
				if (token.IsCancellationRequested) return;
			}
			// Add silence at the start to make room for effects.
			Audio.AudioHelper.WriteSilenceBytes(writer, channelCount, sampleRate, bitsPerSample, silenceStart);
			writer.Write(bytes);
			// Add silence at the back to make room for effects.
			Audio.AudioHelper.WriteSilenceBytes(writer, channelCount, sampleRate, bitsPerSample, silenceEnd);
			// Add result to play list.
			item.WavData = ms.GetBuffer();
			//System.IO.File.WriteAllBytes("Temp.wav", item.Data);
			var duration = ((decimal)bytes.Length * 8m) / (decimal)channelCount / (decimal)sampleRate / (decimal)bitsPerSample * 1000m;
			duration += (silenceStart + silenceEnd);
			item.Duration = (int)duration;
		}

		Exception _LastException;
		Exception LastException
		{
			get { return _LastException; }
			set
			{
				_LastException = value;
				if (Disposing || IsDisposed || !IsHandleCreated)
				{
					var errorText = ((Exception)_LastException).ToString();
					System.IO.File.WriteAllText("JocysCom.TextToSpeech.Monitor.Error2.txt", errorText);
				}
				else
				{
					BeginInvoke((Action)(() =>
					{
						ErrorToolStripStatusLabel.Text = value == null ? "" : value.Message;
						ErrorToolStripStatusLabel.Visible = value != null;

					}));
				}
			}
		}

		#region Network

		// The socket which monitors all incoming packets.
		private List<Socket> monitoringSockets = new List<Socket>();
		private byte[] byteData = new byte[0xFFFF];
		// A flag to check if packets are to be monitored or not.
		private bool continueMonitoring = false;
		List<IPAddress> IpAddresses = new List<IPAddress>();
		object monitorLock = new object();

		private void StartNetworkMonitor()
		{
			lock (monitorLock)
			{
				// If monitor is running already then...
				if (monitoringSockets.Count > 0)
				{
					MonitoringStateStatusLabel.Text = "Error: Monitoring already. Stop first!";
					return;
				}
				IPHostEntry HosyEntry = Dns.GetHostEntry((Dns.GetHostName()));
				IpAddresses.Clear();
				if (HosyEntry.AddressList.Length > 0)
				{
					foreach (IPAddress ip in HosyEntry.AddressList)
					{
						if (ip.AddressFamily == AddressFamily.InterNetwork)
						{
							// If IP address is not in the list then...
							if (!IpAddresses.Contains(ip))
							{
								// Add IP Address.
								IpAddresses.Add(ip);
							}
						}
					}
				}
				if (IpAddresses.Count == 0)
				{
					MonitoringStateStatusLabel.Text = "Error: No Interface to monitor the packets!";
					return;
				}
				else
				{
					MonitoringStateStatusLabel.Text = string.Format("Monitoring: {0}", string.Join(", ", IpAddresses));
				}
				try
				{
					var monitorPort = (int)PortNumericUpDown.Value;
					continueMonitoring = true;
					foreach (var ip in IpAddresses)
					{
						// For sniffing the socket to monitor the packets has to be a raw socket, with the
						// address family being of type internetwork, and protocol being IP
						var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
						//Bind the socket to the selected IP address.
						// Note: it looks like monitorPort value is ignored and all ports will be monitored.
						socket.Bind(new IPEndPoint(ip, monitorPort));
						//Set the socket  options: Applies only to TCP packets, Set the include the header, option to true.
						socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
						// Input data required by the operation. 
						byte[] optionInValue = new byte[4] { 1, 0, 0, 0 };
						// Output data returned by the operation. 
						byte[] optionOutValue = new byte[4];
						// Socket.IOControl is analogous to the WSAIoctl method of Winsock 2: Equivalent to SIO_RCVALL constant, of Winsock 2
						socket.IOControl(IOControlCode.ReceiveAll, optionInValue, optionOutValue);
						// Start receiving the packets asynchronously.
						socket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(BeginReceive_Callback), socket);
						monitoringSockets.Add(socket);
					}
				}
				catch (Exception ex)
				{
					LastException = ex;
				}
			}
		}

		void StopNetworkMonitor()
		{
			lock (monitorLock)
			{
				// If monitor is not running then...
				if (monitoringSockets.Count == 0)
				{
					MonitoringStateStatusLabel.Text = "Error: Not monitoring. Start monitor first!";
					return;
				}
				foreach (var socket in monitoringSockets)
				{
					socket.Close();
				}
				monitoringSockets.Clear();
			}
		}

		object PacketsStateStatusLabelLock = new object();
		long PacketsCount = 0;

		private void BeginReceive_Callback(IAsyncResult ar)
		{
			try
			{
				Interlocked.Add(ref PacketsCount, 1);
				if (Disposing || IsDisposed || !IsHandleCreated)
				{
					return;
				}
				BeginInvoke((Action)(() =>
				{
					lock (PacketsStateStatusLabelLock)
					{
						PacketsCount++;
						PacketsStateStatusLabel.Text = string.Format("Packets: {0}", PacketsCount);
					}
				}));
				var socket = (Socket)ar.AsyncState;
				SocketError errorCode;
				int received = socket.EndReceive(ar, out errorCode);
				if (errorCode == SocketError.Success)
				{
					// Analyse the bytes received...
					ParseData(byteData, received);
				}
				if (continueMonitoring)
				{
					// Maximum length of an IP datagram is 65,535 bytes
					byteData = new byte[0xFFFF];
					//Another call to BeginReceive so that we continue to receive the incoming packets.
					socket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(BeginReceive_Callback), socket);
				}
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception ex)
			{
				LastException = ex;
			}
		}


		BindingList<WowListItem> WowMessageList = new BindingList<WowListItem>();

		object SequenceNumbersLock = new object();
		List<uint> SequenceNumbers = new List<uint>();

		private void ParseData(byte[] byteData, int nReceived)
		{
			// All protocol packets are encapsulated in the IP datagram.
			// Parse IP header and see what protocol data is being carried by it.
			IpHeader ipHeader = new IpHeader(byteData, 0, nReceived);
			// If IP datagram contains carries TCP data then...
			if (ipHeader.Protocol == ProtocolType.Tcp)
			{
				var sourceIsLocal = IpAddresses.Contains(ipHeader.SourceAddress);
				var destinationIsLocal = IpAddresses.Contains(ipHeader.DestinationAddress);
				// If message was sent from local IP Address to remote IP address then...
				if (sourceIsLocal && !destinationIsLocal)
				{
					var portNumber = (ushort)PortNumericUpDown.Value;
					// IPHeader.Data stores the data being carried by the IP datagram.
					TcpHeader tcpHeader = new TcpHeader(ipHeader.Data, 0, ipHeader.Data.Length);
					// If message was sent to specified port then...
					if (tcpHeader.DestinationPort == portNumber)
					{
						var wowItem = new WowListItem(ipHeader, tcpHeader);
						// If data contains XML message then...
						if (wowItem.IsVoiceItem)
						{
							lock (SequenceNumbersLock)
							{
								while (SequenceNumbers.Count > 10) SequenceNumbers.RemoveAt(0);
								if (!SequenceNumbers.Contains(tcpHeader.SequenceNumber))
								{
									SequenceNumbers.Add(tcpHeader.SequenceNumber);
									// Add wow item to the list. Use Invoke to make it Thread safe.
									this.Invoke((Action<WowListItem>)addWowListItem, new object[] { wowItem });
								}
							}
						}
					}
				}
			}
		}

		bool ScrollMessagesGrid = false;
		object ScrollMessagesGridLock = new object();

		private void addWowListItem(WowListItem wowItem)
		{
			lock (ScrollMessagesGridLock)
			{
				ScrollMessagesGrid = ScrollGrid();
				// Leave maximum 100 items in the list.
				while (WowMessageList.Count > 100)
				{
					WowMessageList.RemoveAt(0);
				}
				// Add new item at the bottom.
				WowMessageList.Add(wowItem);
			}
			ProcessWowMessage(wowItem.VoiceXml);

		}

		bool ScrollGrid()
		{
			bool scroll;
			lock (ScrollMessagesGridLock)
			{
				var grid = MessagesDataGridView;
				int firstDisplayed = grid.FirstDisplayedScrollingRowIndex;
				int displayed = grid.DisplayedRowCount(true);
				int lastVisible = (firstDisplayed + displayed) - 1;
				int lastIndex = grid.RowCount - 1;
				int newIndex = firstDisplayed + 1;
				scroll = lastVisible == lastIndex;
			}
			return scroll;
		}

		void ScrollDownGrid(DataGridView grid)
		{
			lock (ScrollMessagesGridLock)
			{
				if (ScrollMessagesGrid) { grid.FirstDisplayedScrollingRowIndex = grid.RowCount - 1; }
			}
		}

		private void MessagesDataGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			ScrollDownGrid(MessagesDataGridView);
		}

		private void MessagesDataGridView_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
		{
			ScrollDownGrid(MessagesDataGridView);
		}

		private void SnifferForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (continueMonitoring)
			{
				foreach (var socket in monitoringSockets)
				{
					socket.Close();
				}
			}
		}

		string _Gender;
		string language;
		VoiceGender gender;
		int _Pitch;
		int _PitchComment;
		int _Rate;
		int _Volume;
		string buffer = "";

		void resetBuffer()
		{
			buffer = "";
		}

		void ProcessWowMessage(string text)
		{
			// If <message.
			if (!text.Contains("<message")) return;
			var v = Serializer.DeserializeFromXmlString<message>(text);
			// Override voice values.
			var name = v.name;
			var overrideVoice = SettingsFile.Current.Overrides.FirstOrDefault(x => x.name == name);
			// if override was not found then...
			if (overrideVoice != null) v.OverrideFrom(overrideVoice);

			//Set gender. "Male"(1), "Female"(2), "Neutral"(3).
			_Gender = string.IsNullOrEmpty(v.gender) || v.gender != "Male" && v.gender != "Female" && v.gender != "Neutral" ? GenderComboBox.Text : v.gender;
			gender = (VoiceGender)Enum.Parse(typeof(VoiceGender), _Gender);
			IncomingGenderTextBox.Text = string.IsNullOrEmpty(v.gender) ? "" : "gender=\"" + _Gender + "\"";

			// Set language. ----------------------------------------------------------------------------------------------------
			language = v.language;
			IncomingLanguageTextBox.Text = string.IsNullOrEmpty(language) ? "" : "language=\"" + language + "\"";

			// Set voice. ----------------------------------------------------------------------------------------------------
			SelectVoice(v.name, language, gender);
			IncomingNameTextBox.Text = string.IsNullOrEmpty(v.name) ? "" : "name=\"" + v.name + "\"";

			// Set pitch.
			var pitchIsValid = int.TryParse(v.pitch, out _Pitch);
			if (!pitchIsValid)
			{
				if (int.Parse(PitchMinComboBox.Text) > int.Parse(PitchMaxComboBox.Text))
				{
					_Pitch = MainHelper.GetNumber(int.Parse(PitchMaxComboBox.Text), int.Parse(PitchMinComboBox.Text), "pitch", v.name);
				}
				else
				{
					_Pitch = MainHelper.GetNumber(int.Parse(PitchMinComboBox.Text), int.Parse(PitchMaxComboBox.Text), "pitch", v.name);
				}
			}
			if (_Pitch < -10) _Pitch = -10;
			if (_Pitch > 10) _Pitch = 10;
			IncomingPitchTextBox.Text = pitchIsValid ? "pitch=\"" + _Pitch + "\"" : "";
			PitchTextBox.Text = pitchIsValid ? PitchTextBox.Text = "" : _Pitch.ToString();

			// Set PitchComment.
			_PitchComment = _Pitch >= 0 ? -4 : 4;

			// Set rate.
			var rateIsValid = int.TryParse(v.rate, out _Rate);
			if (!rateIsValid)
			{
				if (int.Parse(RateMinComboBox.Text) > int.Parse(RateMaxComboBox.Text))
				{
					_Rate = MainHelper.GetNumber(int.Parse(RateMaxComboBox.Text), int.Parse(RateMinComboBox.Text), "rate", v.name);
				}
				else
				{
					_Rate = MainHelper.GetNumber(int.Parse(RateMinComboBox.Text), int.Parse(RateMaxComboBox.Text), "rate", v.name);
				}
			}
			if (_Rate < -10) _Rate = -10;
			if (_Rate > 10) _Rate = 10;
			IncomingRateTextBox.Text = rateIsValid ? "rate=\"" + _Rate + "\"" : "";
			RateTextBox.Text = rateIsValid ? RateTextBox.Text = "" : _Rate.ToString();

			// Set effect.
			var effectValue = string.IsNullOrEmpty(v.effect) ? "Default" : v.effect;
			SelectEffectsPreset(effectValue);
			IncomingEffectTextBox.Text = !string.IsNullOrEmpty(v.effect) ? "effect=\"" + v.effect + "\"" : "";

			// Set volume.
			var volumeIsValid = int.TryParse(v.volume, out _Volume);
			if (_Volume < 0) _Volume = 0;
			if (_Volume > 100) _Volume = 100;
			if (!volumeIsValid) _Volume = VolumeTrackBar.Value;
			IncomingVolumeTextBox.Text = volumeIsValid ? "volume=\"" + _Volume + "\"" : "";

			// Set group.
			var groupValue = string.IsNullOrEmpty(v.group) ? "" : v.group;
			IncomingGroupTextBox.Text = !string.IsNullOrEmpty(v.group) ? "group=\"" + v.group + "\"" : "";

			// Set command.
			IncomingCommandTextBox.Text = (string.IsNullOrEmpty(v.command)) ? "command value was not submitted!" : "command=\"" + v.command + "\"";
			if (string.IsNullOrEmpty(v.command)) return;

			// commands.
			switch (v.command.ToLower())
			{
				case "copy":
					break;
				case "add":
					if (v.parts != null) buffer += string.Join("", v.parts);
					break;
				case "sound":
					// Get WAV (name), selected as default.
					string introSound = (string)DefaultIntroSoundComboBox.SelectedItem.ToString().ToLower();
					// If group is not submitted.
					if (string.IsNullOrEmpty(v.group))
					{
						var stream = GetIntroSound(introSound);
						if (stream != null)
						{
							AddIntroSoundToPlayList(introSound, v.group, stream);
						}
					}
					// If group is submitted.
					else
					{
						// Check if group is listed in [Intro Sounds] tab.
						var Sound = SettingsFile.Current.Sounds.FirstOrDefault(x => x.group.ToLower() == v.group.ToLower());
						// If group is not listed.
						if (Sound == null)
						{
							// add group to list.
							SoundsPanel.SoundsAddNewRecord(false, v.group);
							SoundsPanel.SelectRow(v.group);
							// play default (embedded) WAV.
							var stream = GetIntroSound(introSound);
							if (stream != null)
							{
								AddIntroSoundToPlayList(introSound, v.group, stream);
							}
						}
						// If group is listed.
						else
						{
							SoundsPanel.SelectRow(Sound.group);
							if (Sound.enabled == false) break;
							// Get WAV name/path.
							string wavToPlay = string.IsNullOrEmpty(Sound.file) ? introSound : Sound.file;
							wavToPlay = MainHelper.ConvertFromSpecialFoldersPattern(wavToPlay);
							//DefaultIntroSoundComboBox_SelectedIndexChanged(null, null, wavToPlay);
							// byte[] wavData;
							var stream = GetIntroSound(wavToPlay);
							if (stream != null)
							{
								AddIntroSoundToPlayList(wavToPlay, v.group, stream);
							}
							else if (System.IO.File.Exists(wavToPlay))
							{
								stream = new System.IO.FileStream(wavToPlay, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
								AddIntroSoundToPlayList(wavToPlay, v.group, stream);
							}
						}
					}
					break;
				case "play":
					if (v.parts != null) buffer += string.Join("", v.parts);
					var decodedText = System.Web.HttpUtility.HtmlDecode(buffer);
					buffer = "";
					IncomingTextTextBox.Text = decodedText;
                    NVDA_MSG = decodedText;
                    //hook
                    

                    //if ((GetKeyState((int)Keys.CapsLock)) == 1)
                    //{
                        //MessageBox.Show("Capslock is ON");
                    /*if(notFirst == true){
                        //MessageBox.Show("notFirst IS true");

                        keybd_event(VK_CAPSLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYDOWN, 0);
                        keybd_event(VK_CAPSLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0); 
                    }*/
                    //}

                    concurrentCall();
                    //notFirst = true;
                    //keybd_event(VK_CAPSLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYDOWN, 0);
                    //keybd_event(VK_CAPSLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0); 

                    //TextXmlTabControl.SelectedTab = TextTabPage;
                    // mark text (or audio file) with v.goup value.

                    // Add silence before message.
                    int silenceIntBefore = Decimal.ToInt32(OptionsPanel.silenceBefore);
                    string silenceStringBefore = OptionsPanel.silenceBefore.ToString();
                    if (silenceIntBefore > 0) AddTextToPlaylist("<silence msec=\"" + silenceStringBefore + "\" />", true, v.group);

                    //AddTextToPlaylist(decodedText, true, v.group);
                    //StopPlayer();

                    // Add silence after message.
                    int silenceIntAfter = Decimal.ToInt32(OptionsPanel.silenceAfter);
                    string silenceStringAfter = OptionsPanel.silenceAfter.ToString();
                    if (silenceIntAfter > 0) AddTextToPlaylist("<silence msec=\"" + silenceStringAfter + "\" />", true, v.group);


                    break;
				case "stop":
					text = "";
					StopPlayer(v.group);
					IncomingRateTextBox.Text = "";
					IncomingPitchTextBox.Text = "";
					break;
				case "save":
					if (!string.IsNullOrEmpty(v.name)) VoiceOverridesPanel.UpsertRecord(v);
					break;
				default:
					break;
			}
		}

		#endregion

		#region Speech

		BindingList<InstalledVoiceEx> InstalledVoices;

		void InitializeSpeech()
		{
			AudioChannelsComboBox.DataSource = Enum.GetValues(typeof(AudioChannel));
			AudioChannelsComboBox.SelectedItem = AudioChannel.Mono;
			AudioSampleRateComboBox.DataSource = new int[] { 11025, 22050, 44100, 48000 };
			AudioSampleRateComboBox.SelectedItem = 22050;
			AudioBitsPerSampleComboBox.DataSource = new int[] { 16 };
			AudioBitsPerSampleComboBox.SelectedItem = 16;
			// Fill grid with voices.
			// Create synthesizer which will be used to create WAV files from SSML XML.
			var ssmlSynthesizer = new SpeechSynthesizer();
			InstalledVoice[] voices = ssmlSynthesizer.GetInstalledVoices().OrderBy(x => x.VoiceInfo.Culture.Name).ThenBy(x => x.VoiceInfo.Gender).ThenBy(x => x.VoiceInfo.Name).ToArray();
			var voicesEx = voices.Select(x => new InstalledVoiceEx(x.VoiceInfo)).ToArray();
			LoadSettings(voicesEx);
			foreach (var voiceEx in voicesEx) InstalledVoices.Add(voiceEx);
			ssmlSynthesizer.Dispose();
			ssmlSynthesizer = null;
			VoicesDataGridView.DataSource = InstalledVoices;
			refreshPresets();
		}

		string GetXmlText(string text, InstalledVoiceEx vi, int volume, int pitch, int rate, bool isComment)
		{
			string xml;
			string name = vi.Name;
			var sw = new StringWriter();
			var w = new XmlTextWriter(sw);
			w.Formatting = Formatting.Indented;
			w.WriteStartElement("voice");
			if (string.IsNullOrEmpty(language) || vi.CultureLCID.ToString("X3") != language)
			{
				w.WriteAttributeString("required", "name=" + name);
			}
			else
			{
				w.WriteAttributeString("required", "name=" + name + ";language=" + language); //+ vi.CultureLCID.ToString("X3"));
			}
			w.WriteStartElement("volume");
			w.WriteAttributeString("level", volume.ToString());
			w.WriteStartElement("rate");
			w.WriteAttributeString("absspeed", rate.ToString());
			w.WriteStartElement("pitch");
			w.WriteAttributeString("absmiddle", (isComment ? _PitchComment : pitch).ToString());
			w.WriteRaw(text);
			w.WriteEndElement();
			w.WriteEndElement();
			w.WriteEndElement();
			w.WriteEndElement();
			xml = sw.ToString();
			w.Close();
			return xml;
		}

		// Demonstrates SetText, ContainsText, and GetText. 
		public String SwapClipboardHtmlText(String replacementHtmlText)
		{
			String returnHtmlText = null;
			if (Clipboard.ContainsText(TextDataFormat.Html))
			{
				returnHtmlText = Clipboard.GetText(TextDataFormat.Html);
				Clipboard.SetText(replacementHtmlText, TextDataFormat.Html);
			}
			return returnHtmlText;
		}

		List<PlayItem> AddTextToPlaylist(string text, bool addToPlaylist, string voiceGroup)
		{
			// It will take too long to convert large amount of text to WAV data and apply all filters.
			// This function will split text into smaller sentences.
			var cs = "[comment]";
			var ce = "[/comment]";
			var items = new List<PlayItem>();
			var splitItems = MainHelper.SplitText(text, new string[] { ". ", "! ", "? ", cs, ce });
			var sentences = splitItems.Where(x => (x.Value + x.Key).Trim().Length > 0).ToArray();
			bool comment = false;
			// Loop trough each sentence.
			for (int i = 0; i < sentences.Length; i++)
			{
				var block = sentences[i];
				// Combine sentence and separator.
				var sentence = block.Value + block.Key.Replace(cs, "").Replace(ce, "");
				if (!string.IsNullOrEmpty(sentence))
				{
					var item = new PlayItem(this)
					{
						Text = sentence,
						Xml = ConvertTextToSapiXml(sentence, comment),
						Status = JobStatusType.Parsed,
						IsComment = comment,
						Group = voiceGroup,
					};
					items.Add(item);
					if (addToPlaylist) lock (playlistLock) { playlist.Add(item); }
				};
				if (block.Key == cs) comment = true;
				if (block.Key == ce) comment = false;
			}
			return items;
		}

		string ConvertTextToSapiXml(string text, bool isComment = false)
		{
			var vi = SelectedVoice;
			return GetXmlText(text, vi, _Volume, _Pitch, _Rate, isComment);
		}

		byte[] ConvertSapiXmlToWav(string xml)
		{
			int bitsPerSample = 0;
			int sampleRate = 0;
			int channelCount = 0;
			Invoke((Action)(() =>
			{
				LastException = null;
				channelCount = (int)(AudioChannel)AudioChannelsComboBox.SelectedItem;
				sampleRate = (int)AudioSampleRateComboBox.SelectedItem;
				bitsPerSample = (int)AudioBitsPerSampleComboBox.SelectedItem;
			}));
			SpeechAudioFormatType t = SpeechAudioFormatType.SAFT48kHz16BitMono;
			switch (channelCount)
			{
				case 1: // Mono
					switch (sampleRate)
					{
						case 11025: t = bitsPerSample == 8 ? SpeechAudioFormatType.SAFT11kHz8BitMono : SpeechAudioFormatType.SAFT11kHz16BitMono; break;
						case 22050: t = bitsPerSample == 8 ? SpeechAudioFormatType.SAFT22kHz8BitMono : SpeechAudioFormatType.SAFT22kHz16BitMono; break;
						case 44100: t = bitsPerSample == 8 ? SpeechAudioFormatType.SAFT44kHz8BitMono : SpeechAudioFormatType.SAFT44kHz16BitMono; break;
						case 48000: t = bitsPerSample == 8 ? SpeechAudioFormatType.SAFT48kHz8BitMono : SpeechAudioFormatType.SAFT48kHz16BitMono; break;
					}
					break;
				case 2: // Stereo
					switch (sampleRate)
					{
						case 11025: t = bitsPerSample == 8 ? SpeechAudioFormatType.SAFT11kHz8BitStereo : SpeechAudioFormatType.SAFT11kHz16BitStereo; break;
						case 22050: t = bitsPerSample == 8 ? SpeechAudioFormatType.SAFT22kHz8BitStereo : SpeechAudioFormatType.SAFT22kHz16BitStereo; break;
						case 44100: t = bitsPerSample == 8 ? SpeechAudioFormatType.SAFT44kHz8BitStereo : SpeechAudioFormatType.SAFT44kHz16BitStereo; break;
						case 48000: t = bitsPerSample == 8 ? SpeechAudioFormatType.SAFT48kHz8BitStereo : SpeechAudioFormatType.SAFT48kHz16BitStereo; break;
					}
					break;
			}
			var voice = new SpeechLib.SpVoice();
			//// Write to file.
			//var fileStream = new SpeechLib.SpFileStream();
			//fileStream.Open("speak.wav", SpeechLib.SpeechStreamFileMode.SSFMCreateForWrite, false);
			//voice.AudioOutputStream = fileStream;
			//voice.Voice = voice.GetVoices().Item(0);
			//voice.Volume = 100;
			//voice.Speak(xml, SpeechLib.SpeechVoiceSpeakFlags.SVSFDefault);
			//MessageBox.Show(voice.AudioOutputStream.Format.Type.ToString());
			//voice = null;
			//fileStream.Close();
			//fileStream = null;
			// Write into memory.
			var stream = new SpeechLib.SpMemoryStream();
			stream.Format.Type = t;
			voice.AudioOutputStream = stream;
			try
			{
				voice.Speak(xml, SpeechVoiceSpeakFlags.SVSFPurgeBeforeSpeak);
			}
			catch (Exception ex)
			{
				LastException = ex;
				return null;
			}
			var spStream = (SpMemoryStream)voice.AudioOutputStream;
			spStream.Seek(0, SpeechStreamSeekPositionType.SSSPTRelativeToStart);
			var bytes = (byte[])(object)spStream.GetData();
			return bytes;
		}

		BindingList<PlayItem> playlist;
		object playlistLock = new object();
		CancellationTokenSource token;

		public void AddMessageToPlay(string text)
		{
			if (!string.IsNullOrEmpty(text))
			{
				if (text.StartsWith("<message"))
				{
					var wowItem = new WowListItem(text);
					addWowListItem(wowItem);
				}
				else
				{
					var item = new PlayItem(this)
					{
						Text = "SAPI XML",
						Xml = text,
						Status = JobStatusType.Parsed,
					};
					lock (playlistLock) { playlist.Add(item); }
				}
			}
		}


        //added by Jehoshua Josue
        void concurrentCall()
        {
            
            #region ParallelTasks
            // Perform three tasks in parallel on the source array
            Parallel.Invoke(() =>
                            {
                                //send to socket                                
                                sendToNVDA(NVDA_MSG);
                                //GetLongestWord(words);
                            },  // close first Action

                            () =>
                            {
                                //INPUT[] Inputs = new INPUT[1];
                                //INPUT Input = new INPUT();
                                ////simulate keypress
                                //Input.type = 1; // 1 = Keyboard Input
                                //Input.U.ki.wScan = ScanCodeShort.CAPITAL;
                                //Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
                                //Inputs[0] = Input;

                                //SendInput(1, Inputs, INPUT.Size);
                                keybd_event(VK_CAPSLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYDOWN, 0);
                                
                            },

                            () =>
                            {
                                INPUT[] Inputs = new INPUT[1];
                                INPUT Input2 = new INPUT();
                                Input2.type = 1; // 1 = Keyboard Input
                                Input2.U.ki.wScan = ScanCodeShort.DIVIDE;
                                Input2.U.ki.dwFlags = KEYEVENTF.SCANCODE;
                                Inputs[0] = Input2;

                                SendInput(1, Inputs, INPUT.Size);
                            }
                ); //close parallel.invoke
            #endregion

            //toggle the CAPSLOCK back off
            //keybd_event(VK_CAPSLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYDOWN, 0);
            keybd_event(VK_CAPSLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0); 
        }


        void SendInputWithAPI()
        {
            INPUT[] Inputs = new INPUT[4];
            INPUT Input = new INPUT();

            Input.type = 1; // 1 = Keyboard Input
            Input.U.ki.wScan = ScanCodeShort.KEY_W;
            Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
            Inputs[0] = Input;

            Input.type = 1; // 1 = Keyboard Input
            Input.U.ki.wScan = ScanCodeShort.KEY_S;
            Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
            Inputs[1] = Input;

            Input.type = 1; // 1 = Keyboard Input
            Input.U.ki.wScan = ScanCodeShort.KEY_A;
            Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
            Inputs[2] = Input;

            Input.type = 1; // 1 = Keyboard Input
            Input.U.ki.wScan = ScanCodeShort.KEY_D;
            Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
            Inputs[3] = Input;


            SendInput(4, Inputs, INPUT.Size);
        }
        

        //used for turning off the CAPSLOCK
        [DllImport("user32.dll", EntryPoint = "keybd_event")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const byte VK_NUMLOCK = 0x90;
        private const byte VK_CAPSLOCK = 0x14;
        private const uint KEYEVENTF_EXTENDEDKEY = 1;
        private const int KEYEVENTF_KEYUP = 0x2;
        private const int KEYEVENTF_KEYDOWN = 0x0;


        /// <summary>
        /// Declaration of external SendInput method
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern uint SendInput(
            uint nInputs,
            [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs,
            int cbSize);


        // Declare the INPUT struct
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            internal uint type;
            internal InputUnion U;
            internal static int Size
            {
                get { return Marshal.SizeOf(typeof(INPUT)); }
            }
        }

        // Declare the InputUnion struct
        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)]
            internal MOUSEINPUT mi;
            [FieldOffset(0)]
            internal KEYBDINPUT ki;
            [FieldOffset(0)]
            internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            internal int dx;
            internal int dy;
            internal MouseEventDataXButtons mouseData;
            internal MOUSEEVENTF dwFlags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }

        [Flags]
        internal enum MouseEventDataXButtons : uint
        {
            Nothing = 0x00000000,
            XBUTTON1 = 0x00000001,
            XBUTTON2 = 0x00000002
        }

        [Flags]
        internal enum MOUSEEVENTF : uint
        {
            ABSOLUTE = 0x8000,
            HWHEEL = 0x01000,
            MOVE = 0x0001,
            MOVE_NOCOALESCE = 0x2000,
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            RIGHTDOWN = 0x0008,
            RIGHTUP = 0x0010,
            MIDDLEDOWN = 0x0020,
            MIDDLEUP = 0x0040,
            VIRTUALDESK = 0x4000,
            WHEEL = 0x0800,
            XDOWN = 0x0080,
            XUP = 0x0100
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            internal VirtualKeyShort wVk;
            internal ScanCodeShort wScan;
            internal KEYEVENTF dwFlags;
            internal int time;
            internal UIntPtr dwExtraInfo;
        }

        [Flags]
        internal enum KEYEVENTF : uint
        {
            EXTENDEDKEY = 0x0001,
            KEYUP = 0x0002,
            SCANCODE = 0x0008,
            UNICODE = 0x0004
        }

        internal enum VirtualKeyShort : short
        {
            ///<summary>
            ///Left mouse button
            ///</summary>
            LBUTTON = 0x01,
            ///<summary>
            ///Right mouse button
            ///</summary>
            RBUTTON = 0x02,
            ///<summary>
            ///Control-break processing
            ///</summary>
            CANCEL = 0x03,
            ///<summary>
            ///Middle mouse button (three-button mouse)
            ///</summary>
            MBUTTON = 0x04,
            ///<summary>
            ///Windows 2000/XP: X1 mouse button
            ///</summary>
            XBUTTON1 = 0x05,
            ///<summary>
            ///Windows 2000/XP: X2 mouse button
            ///</summary>
            XBUTTON2 = 0x06,
            ///<summary>
            ///BACKSPACE key
            ///</summary>
            BACK = 0x08,
            ///<summary>
            ///TAB key
            ///</summary>
            TAB = 0x09,
            ///<summary>
            ///CLEAR key
            ///</summary>
            CLEAR = 0x0C,
            ///<summary>
            ///ENTER key
            ///</summary>
            RETURN = 0x0D,
            ///<summary>
            ///SHIFT key
            ///</summary>
            SHIFT = 0x10,
            ///<summary>
            ///CTRL key
            ///</summary>
            CONTROL = 0x11,
            ///<summary>
            ///ALT key
            ///</summary>
            MENU = 0x12,
            ///<summary>
            ///PAUSE key
            ///</summary>
            PAUSE = 0x13,
            ///<summary>
            ///CAPS LOCK key
            ///</summary>
            CAPITAL = 0x14,
            ///<summary>
            ///Input Method Editor (IME) Kana mode
            ///</summary>
            KANA = 0x15,
            ///<summary>
            ///IME Hangul mode
            ///</summary>
            HANGUL = 0x15,
            ///<summary>
            ///IME Junja mode
            ///</summary>
            JUNJA = 0x17,
            ///<summary>
            ///IME final mode
            ///</summary>
            FINAL = 0x18,
            ///<summary>
            ///IME Hanja mode
            ///</summary>
            HANJA = 0x19,
            ///<summary>
            ///IME Kanji mode
            ///</summary>
            KANJI = 0x19,
            ///<summary>
            ///ESC key
            ///</summary>
            ESCAPE = 0x1B,
            ///<summary>
            ///IME convert
            ///</summary>
            CONVERT = 0x1C,
            ///<summary>
            ///IME nonconvert
            ///</summary>
            NONCONVERT = 0x1D,
            ///<summary>
            ///IME accept
            ///</summary>
            ACCEPT = 0x1E,
            ///<summary>
            ///IME mode change request
            ///</summary>
            MODECHANGE = 0x1F,
            ///<summary>
            ///SPACEBAR
            ///</summary>
            SPACE = 0x20,
            ///<summary>
            ///PAGE UP key
            ///</summary>
            PRIOR = 0x21,
            ///<summary>
            ///PAGE DOWN key
            ///</summary>
            NEXT = 0x22,
            ///<summary>
            ///END key
            ///</summary>
            END = 0x23,
            ///<summary>
            ///HOME key
            ///</summary>
            HOME = 0x24,
            ///<summary>
            ///LEFT ARROW key
            ///</summary>
            LEFT = 0x25,
            ///<summary>
            ///UP ARROW key
            ///</summary>
            UP = 0x26,
            ///<summary>
            ///RIGHT ARROW key
            ///</summary>
            RIGHT = 0x27,
            ///<summary>
            ///DOWN ARROW key
            ///</summary>
            DOWN = 0x28,
            ///<summary>
            ///SELECT key
            ///</summary>
            SELECT = 0x29,
            ///<summary>
            ///PRINT key
            ///</summary>
            PRINT = 0x2A,
            ///<summary>
            ///EXECUTE key
            ///</summary>
            EXECUTE = 0x2B,
            ///<summary>
            ///PRINT SCREEN key
            ///</summary>
            SNAPSHOT = 0x2C,
            ///<summary>
            ///INS key
            ///</summary>
            INSERT = 0x2D,
            ///<summary>
            ///DEL key
            ///</summary>
            DELETE = 0x2E,
            ///<summary>
            ///HELP key
            ///</summary>
            HELP = 0x2F,
            ///<summary>
            ///0 key
            ///</summary>
            KEY_0 = 0x30,
            ///<summary>
            ///1 key
            ///</summary>
            KEY_1 = 0x31,
            ///<summary>
            ///2 key
            ///</summary>
            KEY_2 = 0x32,
            ///<summary>
            ///3 key
            ///</summary>
            KEY_3 = 0x33,
            ///<summary>
            ///4 key
            ///</summary>
            KEY_4 = 0x34,
            ///<summary>
            ///5 key
            ///</summary>
            KEY_5 = 0x35,
            ///<summary>
            ///6 key
            ///</summary>
            KEY_6 = 0x36,
            ///<summary>
            ///7 key
            ///</summary>
            KEY_7 = 0x37,
            ///<summary>
            ///8 key
            ///</summary>
            KEY_8 = 0x38,
            ///<summary>
            ///9 key
            ///</summary>
            KEY_9 = 0x39,
            ///<summary>
            ///A key
            ///</summary>
            KEY_A = 0x41,
            ///<summary>
            ///B key
            ///</summary>
            KEY_B = 0x42,
            ///<summary>
            ///C key
            ///</summary>
            KEY_C = 0x43,
            ///<summary>
            ///D key
            ///</summary>
            KEY_D = 0x44,
            ///<summary>
            ///E key
            ///</summary>
            KEY_E = 0x45,
            ///<summary>
            ///F key
            ///</summary>
            KEY_F = 0x46,
            ///<summary>
            ///G key
            ///</summary>
            KEY_G = 0x47,
            ///<summary>
            ///H key
            ///</summary>
            KEY_H = 0x48,
            ///<summary>
            ///I key
            ///</summary>
            KEY_I = 0x49,
            ///<summary>
            ///J key
            ///</summary>
            KEY_J = 0x4A,
            ///<summary>
            ///K key
            ///</summary>
            KEY_K = 0x4B,
            ///<summary>
            ///L key
            ///</summary>
            KEY_L = 0x4C,
            ///<summary>
            ///M key
            ///</summary>
            KEY_M = 0x4D,
            ///<summary>
            ///N key
            ///</summary>
            KEY_N = 0x4E,
            ///<summary>
            ///O key
            ///</summary>
            KEY_O = 0x4F,
            ///<summary>
            ///P key
            ///</summary>
            KEY_P = 0x50,
            ///<summary>
            ///Q key
            ///</summary>
            KEY_Q = 0x51,
            ///<summary>
            ///R key
            ///</summary>
            KEY_R = 0x52,
            ///<summary>
            ///S key
            ///</summary>
            KEY_S = 0x53,
            ///<summary>
            ///T key
            ///</summary>
            KEY_T = 0x54,
            ///<summary>
            ///U key
            ///</summary>
            KEY_U = 0x55,
            ///<summary>
            ///V key
            ///</summary>
            KEY_V = 0x56,
            ///<summary>
            ///W key
            ///</summary>
            KEY_W = 0x57,
            ///<summary>
            ///X key
            ///</summary>
            KEY_X = 0x58,
            ///<summary>
            ///Y key
            ///</summary>
            KEY_Y = 0x59,
            ///<summary>
            ///Z key
            ///</summary>
            KEY_Z = 0x5A,
            ///<summary>
            ///Left Windows key (Microsoft Natural keyboard) 
            ///</summary>
            LWIN = 0x5B,
            ///<summary>
            ///Right Windows key (Natural keyboard)
            ///</summary>
            RWIN = 0x5C,
            ///<summary>
            ///Applications key (Natural keyboard)
            ///</summary>
            APPS = 0x5D,
            ///<summary>
            ///Computer Sleep key
            ///</summary>
            SLEEP = 0x5F,
            ///<summary>
            ///Numeric keypad 0 key
            ///</summary>
            NUMPAD0 = 0x60,
            ///<summary>
            ///Numeric keypad 1 key
            ///</summary>
            NUMPAD1 = 0x61,
            ///<summary>
            ///Numeric keypad 2 key
            ///</summary>
            NUMPAD2 = 0x62,
            ///<summary>
            ///Numeric keypad 3 key
            ///</summary>
            NUMPAD3 = 0x63,
            ///<summary>
            ///Numeric keypad 4 key
            ///</summary>
            NUMPAD4 = 0x64,
            ///<summary>
            ///Numeric keypad 5 key
            ///</summary>
            NUMPAD5 = 0x65,
            ///<summary>
            ///Numeric keypad 6 key
            ///</summary>
            NUMPAD6 = 0x66,
            ///<summary>
            ///Numeric keypad 7 key
            ///</summary>
            NUMPAD7 = 0x67,
            ///<summary>
            ///Numeric keypad 8 key
            ///</summary>
            NUMPAD8 = 0x68,
            ///<summary>
            ///Numeric keypad 9 key
            ///</summary>
            NUMPAD9 = 0x69,
            ///<summary>
            ///Multiply key
            ///</summary>
            MULTIPLY = 0x6A,
            ///<summary>
            ///Add key
            ///</summary>
            ADD = 0x6B,
            ///<summary>
            ///Separator key
            ///</summary>
            SEPARATOR = 0x6C,
            ///<summary>
            ///Subtract key
            ///</summary>
            SUBTRACT = 0x6D,
            ///<summary>
            ///Decimal key
            ///</summary>
            DECIMAL = 0x6E,
            ///<summary>
            ///Divide key
            ///</summary>
            DIVIDE = 0x6F,
            ///<summary>
            ///F1 key
            ///</summary>
            F1 = 0x70,
            ///<summary>
            ///F2 key
            ///</summary>
            F2 = 0x71,
            ///<summary>
            ///F3 key
            ///</summary>
            F3 = 0x72,
            ///<summary>
            ///F4 key
            ///</summary>
            F4 = 0x73,
            ///<summary>
            ///F5 key
            ///</summary>
            F5 = 0x74,
            ///<summary>
            ///F6 key
            ///</summary>
            F6 = 0x75,
            ///<summary>
            ///F7 key
            ///</summary>
            F7 = 0x76,
            ///<summary>
            ///F8 key
            ///</summary>
            F8 = 0x77,
            ///<summary>
            ///F9 key
            ///</summary>
            F9 = 0x78,
            ///<summary>
            ///F10 key
            ///</summary>
            F10 = 0x79,
            ///<summary>
            ///F11 key
            ///</summary>
            F11 = 0x7A,
            ///<summary>
            ///F12 key
            ///</summary>
            F12 = 0x7B,
            ///<summary>
            ///F13 key
            ///</summary>
            F13 = 0x7C,
            ///<summary>
            ///F14 key
            ///</summary>
            F14 = 0x7D,
            ///<summary>
            ///F15 key
            ///</summary>
            F15 = 0x7E,
            ///<summary>
            ///F16 key
            ///</summary>
            F16 = 0x7F,
            ///<summary>
            ///F17 key  
            ///</summary>
            F17 = 0x80,
            ///<summary>
            ///F18 key  
            ///</summary>
            F18 = 0x81,
            ///<summary>
            ///F19 key  
            ///</summary>
            F19 = 0x82,
            ///<summary>
            ///F20 key  
            ///</summary>
            F20 = 0x83,
            ///<summary>
            ///F21 key  
            ///</summary>
            F21 = 0x84,
            ///<summary>
            ///F22 key, (PPC only) Key used to lock device.
            ///</summary>
            F22 = 0x85,
            ///<summary>
            ///F23 key  
            ///</summary>
            F23 = 0x86,
            ///<summary>
            ///F24 key  
            ///</summary>
            F24 = 0x87,
            ///<summary>
            ///NUM LOCK key
            ///</summary>
            NUMLOCK = 0x90,
            ///<summary>
            ///SCROLL LOCK key
            ///</summary>
            SCROLL = 0x91,
            ///<summary>
            ///Left SHIFT key
            ///</summary>
            LSHIFT = 0xA0,
            ///<summary>
            ///Right SHIFT key
            ///</summary>
            RSHIFT = 0xA1,
            ///<summary>
            ///Left CONTROL key
            ///</summary>
            LCONTROL = 0xA2,
            ///<summary>
            ///Right CONTROL key
            ///</summary>
            RCONTROL = 0xA3,
            ///<summary>
            ///Left MENU key
            ///</summary>
            LMENU = 0xA4,
            ///<summary>
            ///Right MENU key
            ///</summary>
            RMENU = 0xA5,
            ///<summary>
            ///Windows 2000/XP: Browser Back key
            ///</summary>
            BROWSER_BACK = 0xA6,
            ///<summary>
            ///Windows 2000/XP: Browser Forward key
            ///</summary>
            BROWSER_FORWARD = 0xA7,
            ///<summary>
            ///Windows 2000/XP: Browser Refresh key
            ///</summary>
            BROWSER_REFRESH = 0xA8,
            ///<summary>
            ///Windows 2000/XP: Browser Stop key
            ///</summary>
            BROWSER_STOP = 0xA9,
            ///<summary>
            ///Windows 2000/XP: Browser Search key 
            ///</summary>
            BROWSER_SEARCH = 0xAA,
            ///<summary>
            ///Windows 2000/XP: Browser Favorites key
            ///</summary>
            BROWSER_FAVORITES = 0xAB,
            ///<summary>
            ///Windows 2000/XP: Browser Start and Home key
            ///</summary>
            BROWSER_HOME = 0xAC,
            ///<summary>
            ///Windows 2000/XP: Volume Mute key
            ///</summary>
            VOLUME_MUTE = 0xAD,
            ///<summary>
            ///Windows 2000/XP: Volume Down key
            ///</summary>
            VOLUME_DOWN = 0xAE,
            ///<summary>
            ///Windows 2000/XP: Volume Up key
            ///</summary>
            VOLUME_UP = 0xAF,
            ///<summary>
            ///Windows 2000/XP: Next Track key
            ///</summary>
            MEDIA_NEXT_TRACK = 0xB0,
            ///<summary>
            ///Windows 2000/XP: Previous Track key
            ///</summary>
            MEDIA_PREV_TRACK = 0xB1,
            ///<summary>
            ///Windows 2000/XP: Stop Media key
            ///</summary>
            MEDIA_STOP = 0xB2,
            ///<summary>
            ///Windows 2000/XP: Play/Pause Media key
            ///</summary>
            MEDIA_PLAY_PAUSE = 0xB3,
            ///<summary>
            ///Windows 2000/XP: Start Mail key
            ///</summary>
            LAUNCH_MAIL = 0xB4,
            ///<summary>
            ///Windows 2000/XP: Select Media key
            ///</summary>
            LAUNCH_MEDIA_SELECT = 0xB5,
            ///<summary>
            ///Windows 2000/XP: Start Application 1 key
            ///</summary>
            LAUNCH_APP1 = 0xB6,
            ///<summary>
            ///Windows 2000/XP: Start Application 2 key
            ///</summary>
            LAUNCH_APP2 = 0xB7,
            ///<summary>
            ///Used for miscellaneous characters; it can vary by keyboard.
            ///</summary>
            OEM_1 = 0xBA,
            ///<summary>
            ///Windows 2000/XP: For any country/region, the '+' key
            ///</summary>
            OEM_PLUS = 0xBB,
            ///<summary>
            ///Windows 2000/XP: For any country/region, the ',' key
            ///</summary>
            OEM_COMMA = 0xBC,
            ///<summary>
            ///Windows 2000/XP: For any country/region, the '-' key
            ///</summary>
            OEM_MINUS = 0xBD,
            ///<summary>
            ///Windows 2000/XP: For any country/region, the '.' key
            ///</summary>
            OEM_PERIOD = 0xBE,
            ///<summary>
            ///Used for miscellaneous characters; it can vary by keyboard.
            ///</summary>
            OEM_2 = 0xBF,
            ///<summary>
            ///Used for miscellaneous characters; it can vary by keyboard. 
            ///</summary>
            OEM_3 = 0xC0,
            ///<summary>
            ///Used for miscellaneous characters; it can vary by keyboard. 
            ///</summary>
            OEM_4 = 0xDB,
            ///<summary>
            ///Used for miscellaneous characters; it can vary by keyboard. 
            ///</summary>
            OEM_5 = 0xDC,
            ///<summary>
            ///Used for miscellaneous characters; it can vary by keyboard. 
            ///</summary>
            OEM_6 = 0xDD,
            ///<summary>
            ///Used for miscellaneous characters; it can vary by keyboard. 
            ///</summary>
            OEM_7 = 0xDE,
            ///<summary>
            ///Used for miscellaneous characters; it can vary by keyboard.
            ///</summary>
            OEM_8 = 0xDF,
            ///<summary>
            ///Windows 2000/XP: Either the angle bracket key or the backslash key on the RT 102-key keyboard
            ///</summary>
            OEM_102 = 0xE2,
            ///<summary>
            ///Windows 95/98/Me, Windows NT 4.0, Windows 2000/XP: IME PROCESS key
            ///</summary>
            PROCESSKEY = 0xE5,
            ///<summary>
            ///Windows 2000/XP: Used to pass Unicode characters as if they were keystrokes.
            ///The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods. For more information,
            ///see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN, and WM_KEYUP
            ///</summary>
            PACKET = 0xE7,
            ///<summary>
            ///Attn key
            ///</summary>
            ATTN = 0xF6,
            ///<summary>
            ///CrSel key
            ///</summary>
            CRSEL = 0xF7,
            ///<summary>
            ///ExSel key
            ///</summary>
            EXSEL = 0xF8,
            ///<summary>
            ///Erase EOF key
            ///</summary>
            EREOF = 0xF9,
            ///<summary>
            ///Play key
            ///</summary>
            PLAY = 0xFA,
            ///<summary>
            ///Zoom key
            ///</summary>
            ZOOM = 0xFB,
            ///<summary>
            ///Reserved 
            ///</summary>
            NONAME = 0xFC,
            ///<summary>
            ///PA1 key
            ///</summary>
            PA1 = 0xFD,
            ///<summary>
            ///Clear key
            ///</summary>
            OEM_CLEAR = 0xFE
        }

        internal enum ScanCodeShort : short
        {
            LBUTTON = 0,
            RBUTTON = 0,
            CANCEL = 70,
            MBUTTON = 0,
            XBUTTON1 = 0,
            XBUTTON2 = 0,
            BACK = 14,
            TAB = 15,
            CLEAR = 76,
            RETURN = 28,
            SHIFT = 42,
            CONTROL = 29,
            MENU = 56,
            PAUSE = 0,
            CAPITAL = 58,
            KANA = 0,
            HANGUL = 0,
            JUNJA = 0,
            FINAL = 0,
            HANJA = 0,
            KANJI = 0,
            ESCAPE = 1,
            CONVERT = 0,
            NONCONVERT = 0,
            ACCEPT = 0,
            MODECHANGE = 0,
            SPACE = 57,
            PRIOR = 73,
            NEXT = 81,
            END = 79,
            HOME = 71,
            LEFT = 75,
            UP = 72,
            RIGHT = 77,
            DOWN = 80,
            SELECT = 0,
            PRINT = 0,
            EXECUTE = 0,
            SNAPSHOT = 84,
            INSERT = 82,
            DELETE = 83,
            HELP = 99,
            KEY_0 = 11,
            KEY_1 = 2,
            KEY_2 = 3,
            KEY_3 = 4,
            KEY_4 = 5,
            KEY_5 = 6,
            KEY_6 = 7,
            KEY_7 = 8,
            KEY_8 = 9,
            KEY_9 = 10,
            KEY_A = 30,
            KEY_B = 48,
            KEY_C = 46,
            KEY_D = 32,
            KEY_E = 18,
            KEY_F = 33,
            KEY_G = 34,
            KEY_H = 35,
            KEY_I = 23,
            KEY_J = 36,
            KEY_K = 37,
            KEY_L = 38,
            KEY_M = 50,
            KEY_N = 49,
            KEY_O = 24,
            KEY_P = 25,
            KEY_Q = 16,
            KEY_R = 19,
            KEY_S = 31,
            KEY_T = 20,
            KEY_U = 22,
            KEY_V = 47,
            KEY_W = 17,
            KEY_X = 45,
            KEY_Y = 21,
            KEY_Z = 44,
            LWIN = 91,
            RWIN = 92,
            APPS = 93,
            SLEEP = 95,
            NUMPAD0 = 82,
            NUMPAD1 = 79,
            NUMPAD2 = 80,
            NUMPAD3 = 81,
            NUMPAD4 = 75,
            NUMPAD5 = 76,
            NUMPAD6 = 77,
            NUMPAD7 = 71,
            NUMPAD8 = 72,
            NUMPAD9 = 73,
            MULTIPLY = 55,
            ADD = 78,
            SEPARATOR = 0,
            SUBTRACT = 74,
            DECIMAL = 83,
            DIVIDE = 53,
            F1 = 59,
            F2 = 60,
            F3 = 61,
            F4 = 62,
            F5 = 63,
            F6 = 64,
            F7 = 65,
            F8 = 66,
            F9 = 67,
            F10 = 68,
            F11 = 87,
            F12 = 88,
            F13 = 100,
            F14 = 101,
            F15 = 102,
            F16 = 103,
            F17 = 104,
            F18 = 105,
            F19 = 106,
            F20 = 107,
            F21 = 108,
            F22 = 109,
            F23 = 110,
            F24 = 118,
            NUMLOCK = 69,
            SCROLL = 70,
            LSHIFT = 42,
            RSHIFT = 54,
            LCONTROL = 29,
            RCONTROL = 29,
            LMENU = 56,
            RMENU = 56,
            BROWSER_BACK = 106,
            BROWSER_FORWARD = 105,
            BROWSER_REFRESH = 103,
            BROWSER_STOP = 104,
            BROWSER_SEARCH = 101,
            BROWSER_FAVORITES = 102,
            BROWSER_HOME = 50,
            VOLUME_MUTE = 32,
            VOLUME_DOWN = 46,
            VOLUME_UP = 48,
            MEDIA_NEXT_TRACK = 25,
            MEDIA_PREV_TRACK = 16,
            MEDIA_STOP = 36,
            MEDIA_PLAY_PAUSE = 34,
            LAUNCH_MAIL = 108,
            LAUNCH_MEDIA_SELECT = 109,
            LAUNCH_APP1 = 107,
            LAUNCH_APP2 = 33,
            OEM_1 = 39,
            OEM_PLUS = 13,
            OEM_COMMA = 51,
            OEM_MINUS = 12,
            OEM_PERIOD = 52,
            OEM_2 = 53,
            OEM_3 = 41,
            OEM_4 = 26,
            OEM_5 = 43,
            OEM_6 = 27,
            OEM_7 = 40,
            OEM_8 = 0,
            OEM_102 = 86,
            PROCESSKEY = 0,
            PACKET = 0,
            ATTN = 0,
            CRSEL = 0,
            EXSEL = 0,
            EREOF = 93,
            PLAY = 0,
            ZOOM = 98,
            NONAME = 0,
            PA1 = 0,
            OEM_CLEAR = 0,
        }

        /// <summary>
        /// Define HARDWAREINPUT struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            internal int uMsg;
            internal short wParamL;
            internal short wParamH;
        }


        void sendToNVDA(String myMsg)
        {
            byte[] data = new byte[1024];
            //string input;            
            TcpClient server;
            String myIP = GetLocalIPAddress();
            int portNum = 3724;

            try
            {
                server = new TcpClient(myIP, portNum);
            }
            catch (SocketException)
            {
                Console.WriteLine("Unable to connect to server");
                return;
            }
            NetworkStream ns = server.GetStream();

            //int recv = ns.Read(data, 0, data.Length);
            //myMsg = Encoding.ASCII.GetString(data, 0, recv);            
            
           
            //send messge
            ns.Write(Encoding.ASCII.GetBytes(myMsg), 0, myMsg.Length);
            ns.Flush();

        }

        public void simKeyPress(){
            //keypress simulation
            INPUT[] Inputs = new INPUT[1];
            INPUT Input = new INPUT();
            Input.type = 1; // 1 = Keyboard Input
            Input.U.ki.wScan = ScanCodeShort.KEY_A;
            Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
            Inputs[0] = Input;

            SendInput(1, Inputs, INPUT.Size);

        }


        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }



    

        




		void SpeakButton_Click(object sender, EventArgs e)
		{
            tester += 1;

            NVDA_MSG = "brown fox blgf" + tester;
            IncomingTextTextBox.Text = NVDA_MSG;

            concurrentCall();


            //hook2
            


            //toggle the CAPSLOCK back to OFF
            //INPUT[] Inputs = new INPUT[1];
            //INPUT Input = new INPUT();
            //Input.type = 1; // 1 = Keyboard Input
            //Input.U.ki.wScan = ScanCodeShort.CAPITAL;
            //Input.U.ki.dwFlags = KEYEVENTF.KEYUP;
            //Inputs[0] = Input;

            //SendInput(1, Inputs, INPUT.Size);



            

            //// if [ Formatted SAPI XML Text ] tab selected.
            //if (TextXmlTabControl.SelectedTab == SapiTabPage)
            //{
            //    AddMessageToPlay(SapiTextBox.Text);
            //}
            //// if [ SandBox ] tab selected.
            //else if (TextXmlTabControl.SelectedTab == SandBoxTabPage)
            //{
            //    AddMessageToPlay(SandBoxTextBox.Text);
            //}
            //// if [ Incoming Messages ] tab selected.
            //else if (TextXmlTabControl.SelectedTab == MessagesTabPage)
            //{
            //    var gridRow = MessagesDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
            //    if (gridRow != null)
            //    {
            //        var item = (WowListItem)gridRow.DataBoundItem;
            //        ProcessWowMessage(item.VoiceXml);
            //    }
            //}
            //else
            //{
            //    AddTextToPlaylist(IncomingTextTextBox.Text, true, "TextBox");
            //}
		}

		//void ShowVoice(InstalledVoice voice)
		//{
		//    var s = "";
		//    if (voice == null)
		//    {
		//        VoiceDetailsTabPage.ImageKey = "";
		//    }
		//    else
		//    {
		//        var vi = voice.VoiceInfo;
		//        Dictionary<string, object> info = new Dictionary<string, object>();
		//        info.Add("Name", vi.Name);
		//        info.Add("ID", vi.Id);
		//        info.Add("Age", vi.Age);
		//        info.Add("Gender", vi.Gender);
		//        info.Add("Culture", vi.Culture);
		//        info.Add("Enabled", voice.Enabled);
		//        var audioFormats = vi.SupportedAudioFormats.Select(x => x.EncodingFormat).ToArray();
		//        if (audioFormats.Length > 0)
		//        {
		//            info.Add("AudioFormats", string.Join(", ", audioFormats));
		//        }
		//        foreach (string key in vi.AdditionalInfo.Keys)
		//        {
		//            if (info.ContainsKey(key)) continue;
		//            var value = string.Format("{0}", vi.AdditionalInfo[key]);
		//            if (string.IsNullOrEmpty(value)) continue;
		//            info.Add(key, value);
		//        }
		//        info.Add("Description", vi.Description);
		//        var lines = info.Select(x => string.Format("{0,-14}{1}", x.Key + ":", x.Value)).ToArray();
		//        s = string.Join("\r\n", lines);
		//        VoiceDetailsTabPage.ImageKey = voice.VoiceInfo.Gender == VoiceGender.Female ? "businesswoman.png" : "businessman2.png";
		//    }
		//    VoiceDetailsTextBox.Text = s;
		//}

		#endregion

		private void VoicesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex == -1) return;
			var grid = (DataGridView)sender;
			var voice = (InstalledVoiceEx)grid.Rows[e.RowIndex].DataBoundItem;
			var column = VoicesDataGridView.Columns[e.ColumnIndex];
			if (e.ColumnIndex == grid.Columns[AgeColumn.Name].Index)
			{
				if (voice.Age.ToString() == "NotSet") e.Value = "...";
			}
			e.CellStyle.ForeColor = voice.Enabled
				? VoicesDataGridView.DefaultCellStyle.ForeColor
				: System.Drawing.SystemColors.ControlDark;
			e.CellStyle.SelectionBackColor = voice.Enabled
			 ? VoicesDataGridView.DefaultCellStyle.SelectionBackColor
			 : System.Drawing.SystemColors.ControlDark;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			if (DesignMode) return;
			SettingsFile.Current.Load();
			LastException = null;
			VoicesDataGridView.AutoGenerateColumns = false;
			MessagesDataGridView.AutoGenerateColumns = false;
			EffectsPresetsDataGridView.AutoGenerateColumns = false;
			MessagesDataGridView.DataSource = WowMessageList;
			// Enable double buffering to make redraw faster.
			typeof(DataGridView).InvokeMember("DoubleBuffered",
			BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
			null, VoicesDataGridView, new object[] { true });
			resetBuffer();
			InitializeSpeech();
			ValidateList();
			InstalledVoices.ListChanged += InstalledVoices_ListChanged;
			if (MonitorPortCheckBox.Checked)
			{
				StartNetworkMonitor();
				PortNumericUpDown.Enabled = false;
			}
			UpdateClipboardMonitor();
			// Load "JocysCom.TextToSpeech.Monitor.rtf" file
			var stream = MainHelper.GetResource("JocysCom.TextToSpeech.Monitor.rtf");
			var sr = new StreamReader(stream);
			AboutRichTextBox.Rtf = sr.ReadToEnd();
			sr.Close();
		}

		//Make the recognizer ready
		SpeechRecognitionEngine recognitionEngine = null;

		// Set voice.
		void SelectVoice(string name, string language, VoiceGender gender)
		{
			int popularity;
			InstalledVoiceEx[] choice;
			// Get only enabled voices.
			var data = InstalledVoices.Where(x => x.Enabled).ToArray();
			InstalledVoiceEx voice = null;

			// Set voice if only gender value is submitted ("Male", "Female", "Neutral").           
			if (string.IsNullOrEmpty(name))
			{
				// Select first most popular voice in "FemaleColumn", "MaleColumn" or "NeutralColumn".
				// Male.
				if (gender == VoiceGender.Male)
				{
					if (string.IsNullOrEmpty(language))
					{
						voice = data.OrderByDescending(x => x.Male).FirstOrDefault();
					}
					else
					{
						voice = data.OrderByDescending(x => x.Male).Where(x => x.Language == language).FirstOrDefault();
					}
					popularity = voice == null ? 0 : voice.Male;
				}
				// Female.
				else if (gender == VoiceGender.Female)
				{
					if (string.IsNullOrEmpty(language))
					{
						voice = data.OrderByDescending(x => x.Female).FirstOrDefault();
					}
					else
					{
						voice = data.OrderByDescending(x => x.Female).Where(x => x.Language == language).FirstOrDefault();
					}
					popularity = voice == null ? 0 : voice.Female;
				}
				// Neutral.
				else
				{
					if (string.IsNullOrEmpty(language))
					{
						voice = data.OrderByDescending(x => x.Neutral).FirstOrDefault();
					}
					else
					{
						voice = data.OrderByDescending(x => x.Neutral).Where(x => x.Language == language).FirstOrDefault();
					}
					popularity = voice == null ? 0 : voice.Neutral;
				}

				if (string.IsNullOrEmpty(language))
				{
					if (popularity == 0) MainHelpLabel.Text = string.Format("There are no voices enabled in \"{0}\" column. Set popularity value to 100 ( normal usage ) or 101 ( normal usage / favourite ) for one voice at least in \"{0}\" column, to use it as \"{0}\" voice.", gender);
				}
				else
				{
					if (popularity == 0) MainHelpLabel.Text = string.Format("There are no voices enabled in \"{0}\" column with \"Language\" value \"{1}\". Set popularity value to 100 ( normal usage ) for one voice at least in \"{0}\" column with \"Language\" value \"{1}\".", gender, language);
				}
			}
			// Select voice if name and gender values are submitted... ("IVONA 2 Amy") or ("Marshal McBride" and "Male", "Female" or "Neutral").
			else
			{
				// Select specific voice if it exists ("IVONA 2 Amy").
				voice = data.FirstOrDefault(x => x.Name == name);

				// If voice was not found then... generate voice number and assign voice ("Marshal McBride" and "Male", "Female" or "Neutral").
				if (voice == null)
				{
					// Select enabled (with value higher than 0) voices.
					if (string.IsNullOrEmpty(language))
					{
						if (gender == VoiceGender.Male) choice = data.Where(x => x.Male > 0).ToArray();
						else if (gender == VoiceGender.Female) choice = data.Where(x => x.Female > 0).ToArray();
						else choice = data.Where(x => x.Neutral > 0).ToArray();
						if (choice.Length == 0)
						{
							MainHelpLabel.Text = string.Format("There are no voices enabled in \"{0}\" column. Set popularity value to 100 ( normal usage ) for one voice at least in \"{0}\" column, to use it as \"{0}\" voice.", gender);
						}
					}
					else
					{
						if (gender == VoiceGender.Male) choice = data.Where(x => x.Male > 0).Where(x => x.Language == language).ToArray();
						else if (gender == VoiceGender.Female) choice = data.Where(x => x.Female > 0).Where(x => x.Language == language).ToArray();
						else choice = data.Where(x => x.Neutral > 0).Where(x => x.Language == language).ToArray();

						if (choice.Length == 0)
						{
							if (gender == VoiceGender.Male) choice = data.Where(x => x.Male > 0).ToArray();
							else if (gender == VoiceGender.Female) choice = data.Where(x => x.Female > 0).ToArray();
							else choice = data.Where(x => x.Neutral > 0).ToArray();
							MainHelpLabel.Text = string.Format("There are no voices enabled in \"{0}\" column with \"Language\" value \"{1}\". Set popularity value to 100 ( normal usage ) for one voice at least in \"{0}\" column with \"Language\" value \"{1}\".", gender, language);
						}
					}

					// If nothing to choose from then try all.
					if (choice.Length == 0)
					{
						choice = data.ToArray();
					}
					if (choice.Length == 0) return;
					// Generate number for selecting voice.
					var number = MainHelper.GetNumber(0, choice.Count() - 1, "name", name);
					voice = choice[number];
				}

			}

			foreach (DataGridViewRow row in VoicesDataGridView.Rows)
			{
				if (row.DataBoundItem.Equals(voice))
				{
					row.Selected = true;
					VoicesDataGridView.FirstDisplayedCell = row.Cells[0];
					break;
				}
			}
		}

		InstalledVoiceEx SelectedVoice
		{
			get
			{
				var selectedItem = VoicesDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				if (selectedItem == null) return null;
				return (InstalledVoiceEx)selectedItem.DataBoundItem;
			}
		}

		void StopPlayer(string group = null)
		{
			bool groupIsPlaying;
			int itemsLeftToPlay;
			lock (playlistLock) { ClearPlayList(group, out groupIsPlaying, out itemsLeftToPlay); }
			if (groupIsPlaying || itemsLeftToPlay == 0)
			{
				resetBuffer();
				if (token != null) token.Cancel();
				EffectPresetsEditorSoundEffectsControl.StopSound();
				WavPlayer.Stop();
			}
			if (itemsLeftToPlay > 0)
			{
				CheckPlayList(false, true);
			}
		}

		private void StopButton_Click(object sender, EventArgs e)
		{
			StopPlayer();
		}

		private void MainStatusStrip_Click(object sender, EventArgs e)
		{
			if (LastException != null)
			{
				MessageBox.Show(LastException.ToString(), "Last Exception");
			}
		}
		private void TextXmlTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			var en = (TextXmlTabControl.SelectedTab != SapiTabPage);
			RateMinComboBox.Enabled = en;
			RateMaxComboBox.Enabled = en;
			PitchMinComboBox.Enabled = en;
			PitchMaxComboBox.Enabled = en;
			_Volume = VolumeTrackBar.Value;
			VolumeTrackBar.Enabled = en;
			VoicesDataGridView.Enabled = en;
			VoicesDataGridView.DefaultCellStyle.SelectionBackColor = en
				? System.Drawing.SystemColors.Highlight
				: System.Drawing.SystemColors.ControlDark;
			//Fill SandBox Tab if it is empty
			if (string.IsNullOrEmpty(SandBoxTextBox.Text))
			{
				SandBoxTextBox.Text = "<message command=\"Play\" language=\"809\" name=\"Marshal McBride\" gender=\"Male\" effect=\"Humanoid\" group=\"Quest\" pitch=\"0\" rate=\"1\" volume=\"100\"><part>Test text to speech. [comment]Test text to speech.[/comment]</part></message>";
			}

			//Fill SAPI Tab
			if (string.IsNullOrEmpty(IncomingTextTextBox.Text))
			{
                SapiTextBox.Text = ConvertTextToSapiXml("Test text to speech.");
			}
			else
			{
				var blocks = AddTextToPlaylist(IncomingTextTextBox.Text, false, "TextBox");
				SapiTextBox.Text = string.Join("\r\n\r\n", blocks.Select(x => x.Xml));
			}
		}

		private void MessagesClearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			WowMessageList.Clear();
		}

		private void addNewToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var preset = EffectsPreset.NewPreset();
			refreshPresets();
		}

		void refreshPresets()
		{
			var presets = EffectsPreset.GetPresets();
			EffectsPresetsDataGridView.DataSource = presets;
		}

		void SelectEffectsPreset(string name)
		{
			foreach (DataGridViewRow row in EffectsPresetsDataGridView.Rows)
			{
				var preset = (EffectsPreset)row.DataBoundItem;
				if (preset.Name == name)
				{
					row.Selected = true;
					EffectsPresetsDataGridView.FirstDisplayedCell = row.Cells[0];
					break;
				}
			}
		}

		private void EffectsPresetsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			var row = EffectsPresetsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			EffectsPreset preset = null;
			if (row != null)
			{
				preset = (EffectsPreset)row.DataBoundItem;
				EffectPresetsEditorSoundEffectsControl.LoadPresetIntoForm(preset);
			}
			EffectsPresetsEditorTabPage.Text = (preset == null)
				? "Effects Presets Editor"
				: string.Format("Effects Presets Editor: {0}", preset.Name);
		}

		private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			refreshPresets();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
				if (token != null)
				{
					token.Cancel();
					token.Dispose();
				}
				DisposeRecognitionEngine();
			}
			base.Dispose(disposing);
		}

		void DisposeRecognitionEngine()
		{
			if (recognitionEngine != null)
			{
				recognitionEngine.SpeechRecognized -= recognitionEngine_SpeechRecognized;
				recognitionEngine.SpeechRecognitionRejected -= recognitionEngine_SpeechRecognitionRejected;
				recognitionEngine.Dispose();
				recognitionEngine = null;
			}
		}

		private void RecognizeButton_Click(object sender, EventArgs e)
		{
			DisposeRecognitionEngine();
			recognitionEngine = new SpeechRecognitionEngine(); //SelectedVoice.Culture
			//Load grammar
			Choices sentences = new Choices();
			sentences.Add(new string[] { "Yes" });
			sentences.Add(new string[] { "No" });
			GrammarBuilder gBuilder = new GrammarBuilder(sentences);
			Grammar g = new Grammar(gBuilder);
			recognitionEngine.LoadGrammar(g);
			//Add a handler
			recognitionEngine.SpeechRecognitionRejected += recognitionEngine_SpeechRecognitionRejected;
			recognitionEngine.SpeechRecognized += recognitionEngine_SpeechRecognized;
			recognitionEngine.SetInputToDefaultAudioDevice();
			recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
		}


		void recognitionEngine_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
		{
			string text = "";
			foreach (RecognizedWordUnit wordUnit in e.Result.Words)
			{
				text += "Rejected: " + wordUnit.Text + ", " + wordUnit.LexicalForm + ", " + wordUnit.Pronunciation + "\r\n";
			}
			IncomingTextTextBox.Text += text;
		}

		void recognitionEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
		{
			string text = "";
			foreach (RecognizedWordUnit wordUnit in e.Result.Words)
			{
				text += "Recognized: " + wordUnit.Text + ", " + wordUnit.LexicalForm + ", " + wordUnit.Pronunciation + "\r\n";
			}
			IncomingTextTextBox.Text += text;
		}

		private void aboutControl_Load(object sender, EventArgs e)
		{
		}

		// Open URL Links in AboutRichTextBox (About.rtf) when LinkClicked.
		private void AboutRichTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start(e.LinkText);
		}

		private void VoicesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;
			var grid = (DataGridView)sender;
			//var column = VoicesDataGridView.Columns[e.ColumnIndex];
			if (e.ColumnIndex == grid.Columns[EnabledColumn.Name].Index)
			{
				var voice = (InstalledVoiceEx)grid.Rows[e.RowIndex].DataBoundItem;
				voice.Enabled = !voice.Enabled;
				VoicesDataGridView.Invalidate();
			}
			if (e.ColumnIndex == grid.Columns[FemaleColumn.Name].Index) VoicesDataGridView.BeginEdit(true);
			if (e.ColumnIndex == grid.Columns[MaleColumn.Name].Index) VoicesDataGridView.BeginEdit(true);
			if (e.ColumnIndex == grid.Columns[NeutralColumn.Name].Index) VoicesDataGridView.BeginEdit(true);
		}

		private void EffectPresetsEditorSoundEffectsControl_Load(object sender, EventArgs e)
		{

		}

		// ToolTip Main
		private void MouseLeave_MainHelpLabel(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Please download this tool only from trustworthy sources. Make sure that this tool is always signed by verified publisher ( Jocys.com ) with signature issued by trusted certificate authority.";
		}

		// ToolTip MouseHover

		private void RateLabel_MouseHover(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Voice rate ( speed ). Default minimum value is 1. Default maximum value is 1.";
		}

		private void MouseHover_RateMin(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Minimum voice rate ( speed ). Default value is 1.";
		}

		private void MouseHover_RateMax(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Maximum voice rate ( speed ). Default value is 1.";
		}

		private void PitchLabel_MouseHover(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Voice pitch. Default minimum value is 0. Default maximum value is 0.";
		}

		private void MouseHover_PitchMin(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Minimum voice pitch. Default value is 0.";
		}

		private void MouseHover_PitchMax(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Maximum voice pitch. Default value is 0.";
		}

		private void MouseHover_AudioSampleRate(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Audio sample rate. Default value is 22050.";
		}

		private void MouseHover_AudioBitsPerSample(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Audio bits per sample. Default value is 16.";
		}

		private void MouseHover_AudioChannels(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Audio chanels. Default value is Mono.";
		}

		private void MouseHover_EffectPresets(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Configuration of each preset is stored in separate \"PresetName.preset.xml\" file in \"Presets\" folder.";
		}

		private void MouseHover_Voices(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Use voices from this list as \"Male\", \"Female\" or \"Neutral\". Popularity values are from 0 ( don’t use ) ... 50 ( reduced usage ) ... 100 ( normal usage ). Voice with highest value will be selected, if \"name\" is not submitted.";
		}

		private void MouseHover_SandBox(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "In \"SandBox\" tab you can create and test your messages.";
		}

		private void MouseHover_IncomingGroupBox(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Incoming values have priority over values set in \"Pitch [ min - max ]\", \"Rate [ min - max ]\", \"Default Gender:\" fields and with \"Volume:\" slider. If you will submit specific values, Monitor will use them instead of local setup.";
		}

		private void MouseHover_GenderComboBox(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "This gender will be used if gender value is not submitted or submitted value is not \"Male\", \"Female\" or \"Neutral\".";
		}

		private void MouseHover_MonitorClipboardComboBox(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "[ Disabled ] - clipboard monitor is disabled. [ For <message> tags ] - will read clipboard text between <message><part>...</part></message> tags only. [ For all text ] - will read all clipboard text.";
		}

		private void MouseHover_PortNumericUpDown(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Port number. Default value is 3724 ( World of Warcraft ).";
		}

		private void MonitorPortCheckBox_MouseEnter(object sender, EventArgs e)
		{
			MainHelpLabel.Text = "Disable or enable port monitoring. Uncheck to edit port number. Default port number value is 3724 ( World of Warcraft ).";
		}

		// Volume TrackBar value changed
		private void VolumeTrackBar_ValueChanged(object sender, EventArgs e)
		{
			VolumeTextBox.Text = VolumeTrackBar.Value.ToString() + "%";
		}

		private void UpdateButton_Click(object sender, EventArgs e)
		{
			UpdateWebBrowser.Navigate("http://www.jocys.com/files/updates/JocysCom.TextToSpeech.Monitor.html");
		}



		#region Clipboard Monitor

		/// <summary>
		/// Sent when the contents of the clipboard have changed.
		/// </summary>
		private const int WM_CLIPBOARDUPDATE = 0x031D;

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			if (m.Msg == WM_CLIPBOARDUPDATE)
			{
				IDataObject iData = Clipboard.GetDataObject();      // Clipboard's data.

				/* Depending on the clipboard's current data format we can process the data differently.
				 * Feel free to add more checks if you want to process more formats. */
				if (iData.GetDataPresent(DataFormats.Text))
				{
					string text = (string)iData.GetData(DataFormats.Text);

					if (MonitorClipboardComboBox.SelectedIndex == 2 && !text.Contains("<message")) text = "<message command=\"Play\"><part>" + text + "</part></message>";
					if (string.IsNullOrEmpty(text) || !text.Contains("<message")) return;
					var wowItem = new WowListItem(text);
					addWowListItem(wowItem);
					// do something with it
				}
				else if (iData.GetDataPresent(DataFormats.Bitmap))
				{
					//Bitmap image = (Bitmap)iData.GetData(DataFormats.Bitmap);
					// do something with it
				}
			}
		}

		private void MonitorClipboardComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateClipboardMonitor();
		}

		bool MonitoringClipboard;
		object MonitoringClipboardLock = new object();

		void UpdateClipboardMonitor()
		{
			if (MonitorClipboardComboBox.SelectedIndex > 0)
			{
				lock (MonitoringClipboardLock)
				{
					// If not monitoring clipboard then...
					if (!MonitoringClipboard)
					{
						// Add form window to the clipboard's format listener list.
						MonitoringClipboard = NativeMethods.AddClipboardFormatListener(this.Handle);
						// if failed then...
						if (!MonitoringClipboard)
						{
							BeginInvoke((Action)delegate()
							{
								// Set drop down to disabled.
								MonitorClipboardComboBox.SelectedIndex = 0;
							});
						}
					}
				}
			}
			else
			{
				lock (MonitoringClipboardLock)
				{
					if (MonitoringClipboard)
					{
						// Remove our window from the clipboard's format listener list.
						NativeMethods.RemoveClipboardFormatListener(this.Handle);
					}
				}
			}

		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			lock (MonitoringClipboardLock)
			{
				if (MonitoringClipboard)
				{
					// Remove our window from the clipboard's format listener list.
					NativeMethods.RemoveClipboardFormatListener(this.Handle);
				}
			}
			SaveSettings();
			// Save settings
			Properties.Settings.Default.Save();
		}


		#endregion

		#region Load/Save Settings

		public void SaveSettings()
		{
			if (InstalledVoices == null) return;
			var xml = Serializer.SerializeToXmlString(InstalledVoices);
			Properties.Settings.Default.VoicesData = xml;
			SettingsFile.Current.Save();
			//Properties.Settings.Default.Save();
		}

		public void LoadSettings(InstalledVoiceEx[] voices)
		{
			var xml = Properties.Settings.Default.VoicesData;
			InstalledVoiceEx[] savedVoices = null;
			if (!string.IsNullOrEmpty(xml))
			{
				try { savedVoices = Serializer.DeserializeFromXmlString<InstalledVoiceEx[]>(xml); }
				catch (Exception) { }
			}
			if (savedVoices == null) savedVoices = new InstalledVoiceEx[0];
			var newVoices = new List<InstalledVoiceEx>();
			var oldVoices = new List<InstalledVoiceEx>();
			foreach (var voice in voices)
			{
				var savedVoice = savedVoices.FirstOrDefault(x => x.Name == voice.Name && x.Gender == voice.Gender);
				if (savedVoice == null)
				{
					newVoices.Add(voice);
				}
				else
				{
					oldVoices.Add(voice);
					voice.Enabled = savedVoice.Enabled;
					voice.Female = savedVoice.Female;
					voice.Male = savedVoice.Male;
					voice.Neutral = savedVoice.Neutral;
				}
			}
			// If new voices added then...
			if (newVoices.Count > 0)
			{
				var maleIvonaFound = voices.Any(x => x.Name.StartsWith("IVONA") && x.Gender == VoiceGender.Male);
				var femaleIvonaFound = voices.Any(x => x.Name.StartsWith("IVONA") && x.Gender == VoiceGender.Female);
				foreach (var newVoice in newVoices)
				{
					// If new voice is Microsoft then...
					if (newVoice.Name.StartsWith("Microsoft"))
					{
						if (newVoice.Gender == VoiceGender.Male && maleIvonaFound) newVoice.Enabled = false;
						if (newVoice.Gender == VoiceGender.Female && femaleIvonaFound) newVoice.Enabled = false;
					}
				}
				var firstVoiceVoice = newVoices.First();
				// If list doesn't have female voices then use first new voice.
				if (!voices.Any(x => x.Female > 0)) firstVoiceVoice.Female = InstalledVoiceEx.MaxVoice;
				// If list doesn't have male voices then use first new voice.
				if (!voices.Any(x => x.Male > 0)) firstVoiceVoice.Male = InstalledVoiceEx.MaxVoice;
				// If list doesn't have neutral voices then use first voice.
				if (!voices.Any(x => x.Neutral > 0))
				{
					var neutralVoices = voices.Where(x => x.Gender == VoiceGender.Neutral);
					foreach (var neutralVoice in neutralVoices) neutralVoice.Neutral = InstalledVoiceEx.MaxVoice;
					if (neutralVoices.Count() == 0)
					{
						var maleVoices = voices.Where(x => x.Gender == VoiceGender.Male);
						foreach (var maleVoice in maleVoices) maleVoice.Neutral = InstalledVoiceEx.MaxVoice;
						if (maleVoices.Count() == 0)
						{
							var femaleVoices = voices.Where(x => x.Gender == VoiceGender.Female);
							foreach (var femaleVoice in femaleVoices) femaleVoice.Neutral = InstalledVoiceEx.MaxVoice;
						}
					}
				}
			}
		}
		#endregion

		private void VoicesDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
			//if (e.RowIndex == -1) return;
			//var grid = (DataGridView)sender;
			//var voice = (InstalledVoiceEx)grid.Rows[e.RowIndex].DataBoundItem;
			//var column = VoicesDataGridView.Columns[e.ColumnIndex];
			e.Cancel = true;
		}

		private void MonitorPortCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateMonitor();
		}

		void UpdateMonitor()
		{
			PortNumericUpDown.Enabled = !MonitorPortCheckBox.Checked;
			if (MonitorPortCheckBox.Checked)
			{
				StartNetworkMonitor();
			}
			else
			{
				StopNetworkMonitor();
			}
		}

		#region Intro Sounds

		string[] GetIntroSoundNames()
		{
			var prefix = ".Audio.";
			var assembly = Assembly.GetExecutingAssembly();
			var names = assembly.GetManifestResourceNames().Where(x => x.Contains(prefix)).ToArray();
			names = names.Select(x => x.Substring(x.IndexOf(prefix) + prefix.Length).Replace(".wav", "")).ToArray();
			return names;
		}

		Stream GetIntroSound(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			var suffix = (".Audio." + name + ".wav").ToLower();
			var assembly = Assembly.GetExecutingAssembly();
			var fullResourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.ToLower().EndsWith(suffix));
			return fullResourceName == null ? null : assembly.GetManifestResourceStream(fullResourceName);
		}

		void AddIntroSoundToPlayList(string text, string group, Stream stream)
		{
			var item = new PlayItem(this)
			{
				Text = text,
				WavData = new byte[0],
				StreamData = stream,
				Group = group,
				Status = JobStatusType.Pitched,
			};
			lock (playlistLock) { playlist.Add(item); }
		}

		void PlayCurrentIntroSond()
		{
			var intro = (string)DefaultIntroSoundComboBox.SelectedItem;
			var stream = GetIntroSound(intro);
			if (stream != null)
			{
				var player = new System.Media.SoundPlayer();
				player.Stream = stream;
				player.Play();
			}
		}

		private void DefaultIntroSoundComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			PlayCurrentIntroSond();
		}

		#endregion

		#region Silence

		Stream GetSilence(int milliseconds)
		{
			var ms = new MemoryStream();
			var writer = new System.IO.BinaryWriter(ms);
			int channelCount = 0;
			int sampleRate = 0;
			int bitsPerSample = 0;
			bool applyEffects = false;
			float pitchShift = 1.0F;
			Invoke((Action)(() =>
			{
				channelCount = (int)(AudioChannel)AudioChannelsComboBox.SelectedItem;
				sampleRate = (int)AudioSampleRateComboBox.SelectedItem;
				bitsPerSample = (int)AudioBitsPerSampleComboBox.SelectedItem;
				applyEffects = EffectPresetsEditorSoundEffectsControl.GeneralCheckBox.Checked;
				pitchShift = ((float)EffectPresetsEditorSoundEffectsControl.GeneralPitchTrackBar.Value / 100F);

			}));
			var silenceBytes = AudioHelper.GetSilenceByteCount(channelCount, sampleRate, bitsPerSample, milliseconds);
			AudioHelper.WriteHeader(writer, silenceBytes, channelCount, sampleRate, bitsPerSample);
			// Add silence.
			Audio.AudioHelper.WriteSilenceBytes(writer, channelCount, sampleRate, bitsPerSample, milliseconds);
			return ms;
		}

		#endregion

	}
}
