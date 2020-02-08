#Server file for testing purposes
#!/usr/bin/python

import socket
import globalPluginHandler
import speech

class GlobalPlugin(globalPluginHandler.GlobalPlugin):
	def script_readMessage(self, gesture):
		speech.speakMessage("Hello World")
		s = socket.socket()
		host = socket.gethostname()		#local machine name into "host"
		port = 3724
		s.bind((host, port))

		s.listen(5)		#listen for client connection

		flag = True
		while flag == True:
			c, addr = s.accept()	#establish connection with client
			#print 'Received connection from', addr
			#c.send('Thanks for connecting')
			speech.speakMessage("Recieved Message")
			print c.recv(1024)
			speech.speakMessage(c.recv(1024))

			#c.close()			#close the connection

	__gestures={
		"kb:NVDA+A":"readMessage"
	}
