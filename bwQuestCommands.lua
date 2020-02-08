--prints count of quests to screen reader
function bwReturnQuestCount()
	local numEntries, numQuests = GetNumQuestLogEntries()	--retrive quest count 
	
	if(numQuests==1)then	--  account for plurals
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("You have " .. numQuests .. " quest in your quest log.", "WHISPER", nil, name)
	else
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("You have " .. numQuests .. " quests in your quest log.", "WHISPER", nil, name)
	end 
end

--stores quest index to global variable and prints title/completion
function bwSelectQuest(questSelection)
	bwQuestMode = false
	local numEntries, numQuests = GetNumQuestLogEntries()					--get total number of entries, should probably store as global

	if(tonumber(questSelection) <= tonumber(numEntries))then					--error checking *compare won't work if you don't typecast returned values to int
		
		if((questSelection*2) ~= selectedQuest)then											--Is the new selections different from the old one?
			QUESTCHANGE = true
			--SendChatMessage("qchange true", "WHISPER", nil, name)
		else 
			QUESTCHANGE = false
			--SendChatMessage("qchange false", "WHISPER", nil, name)
		end 
		
		selectedQuest = questSelection * 2																--store to global
		local questTitle, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(questSelection * 2)		--retrieve information about index
		SelectQuestLogEntry(selectedQuest)		--selects quest
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("You have selected quest titled " .. questTitle, "WHISPER", nil, name)
		
		if(tonumber(isCollapsed) == 1)then				--Have you completed the quest?
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("This quest has been completed and is ready to turn in.", "WHISPER", nil, name)
		else
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("This quest is unfinished.", "WHISPER", nil, name)
		end
		
		
		
	else
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("Invalid quest selection.", "WHISPER", nil, name)
	end
	
	
end

function bwQuestNext()
	local numEntries, numQuests = GetNumQuestLogEntries()
	if(tonumber(numQuests) == 0)then					
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("No quests in quest log.", "WHISPER", nil, name)
	else
		if(qIndex  < tonumber(numQuests))then 					--cycle through quests, return to index 1 if you reach the end or index n if you reach < 1
			qIndex = qIndex + 1
			local questTitle, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(qIndex * 2)
			JocysCom_SendChatMessageStop(2);
			SendChatMessage(questTitle, "WHISPER", nil, name)
			--bwSelectQuest(qIndex)
		elseif(qIndex == tonumber(numQuests))then 
			qIndex = 1
			local questTitle, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(qIndex * 2)
			JocysCom_SendChatMessageStop(2);
			SendChatMessage(questTitle, "WHISPER", nil, name)
			--bwSelectQuest(qIndex)
		end 
	end
end

function bwQuestPrev()
	local numEntries, numQuests = GetNumQuestLogEntries()
	if(tonumber(numQuests) == 0)then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("No quests in quest log.", "WHISPER", nil, name)
	else
		if(qIndex == 1)then 								--cycle through quests, return to index 1 if you reach the end or index n if you reach < 1
			qIndex = tonumber(numQuests)
			local questTitle, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(qIndex * 2)
			JocysCom_SendChatMessageStop(2);
			SendChatMessage(questTitle, "WHISPER", nil, name)
			--bwSelectQuest(qIndex)
		elseif(qIndex  > 1)then 
			qIndex = qIndex - 1
			local questTitle, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(qIndex * 2)
			JocysCom_SendChatMessageStop(2);
			SendChatMessage(questTitle, "WHISPER", nil, name)
			--bwSelectQuest(qIndex)
		end 
	end
end

function bwCycleActivate()
	if(bwQuestMode == false) then			--Allows use of next and prev functions 
		bwQuestMode = true
		bwInvMode = false
		BWSBMODE = false 
		bwRewardMode = false
		bwAcceptOrDeclineMode = false 
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("Press left and right arrow keys to cycle through your quests.  Numpad plus when done", "WHISPER", nil, name)
	else
		bwQuestMode = false
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("Quest selected.  Binding restored.", "WHISPER", nil, name)
		--print("SB mode set to false")
	end
end 

--reads the title of the stored quest value
function bwReadStored()
	if(selectedQuest ~= nil)then 	--return title if there is a stored quest
		local questTitle, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(selectedQuest)
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("Your current quest is titled: " .. questTitle, "WHISPER", nil, name)
	else
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("Please select a quest using the following command.  Forward slash. B W Quest. Select.", "WHISPER", nil, name)		--error message
	end 
	
end

function bwAbandonQuest()
	if(selectedQuest ~= nil)then		
		SelectQuestLogEntry(selectedQuest)		--selects quest to abandon
		SetAbandonQuest()									--begins process
		local abandonName = GetAbandonQuestName()
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("You have abandoned the quest titled: " .. abandonName, "WHISPER", nil, name)
		AbandonQuest()			--finalize abandon
	else
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("Please select a quest to abandon using the following command. Forward slash. B. W. Quest. Number.", "WHISPER", nil, name)		--error message 
	end
end

function bwQuestProgress()
	if(selectedQuest ~= nil)then		--if quest selected 
		SelectQuestLogEntry(selectedQuest)		--move to actually selected 
		local numQuestLogLeaderBoards = GetNumQuestLeaderBoards()	--returns number of objectives for selected quest 
			
		if(tonumber(numQuestLogLeaderBoards) ~= 0)then
			local description, objType, isDone = GetQuestLogLeaderBoard(numQuestLogLeaderBoards, selectedQuest)		--returns usable string
			JocysCom_SendChatMessageStop(2);
			SendChatMessage(description, "WHISPER", nil, name)
		else 
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("Quest is ready to turn in.", "WHISPER", nil, name)
		end 
		
	else
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("Please select a quest with the following command. Forward slash. B. W. Quest. Number.", "WHISPER", nil, name)		--error message
	end
end

function bwLocateObjective()			--unused 
	distance = GetDistanceSqToQuest(selectedQuest)
	JocysCom_SendChatMessageStop(2);
	SendChatMessage(distance, "WHISPER", nil, name)
end 

function bwReadAll()				--test function 
	local questTitleString = " "
	local numEntries, numQuests = GetNumQuestLogEntries()
	
	if(tonumber(numQuests) >= 1)then
		for i=1,tonumber(numQuests) do
			local questTitle, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(i)		--retrieve information about index
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("Quest number " ..i.. " " .. questTitle, "WHISPER", nil, name)
		end
		
	else 
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("No quests in quest log.", "WHISPER", nil, name)		--error message
	end
	
end

--[[function bwSelectReward() 
	if(bwRewardMode == false) then
		bwRewardMode = true 
		bwQuestMode = false
		bwInvMode = false
		BWSBMODE = false 
		SendChatMessage("Press left and right to cycle through quests with multiple rewards, press up and down to review rewards.  If you want more info on the reward, press numpad enter", "WHISPER", nil, name)
	else
		bwRewardMode = false
		SendChatMessage("Reward selected.  Binding restored.", "WHISPER", nil, name)
		--print("SB mode set to false")
	end
end 

function bwRewardRight()
	if(#QUESTREWARDTABLE == 0)then
		SendChatMessage("No quests with rewards to select", "WHISPER", nil, name)
	else
		if(ITEMINDEX  < #QUESTREWARDTABLE)then 
			ITEMINDEX = ITEMINDEX + 1
			bwSelectQuest(QUESTREWARDTABLE[ITEMINDEX])
		elseif(ITEMINDEX == #QUESTREWARDTABLE)then 
			ITEMINDEX = 1
			bwSelectQuest(QUESTREWARDTABLE[ITEMINDEX])
		end 
	end
end

function bwRewardLeft()
	if(#QUESTREWARDTABLE == 0)then
		SendChatMessage("No quests with rewards to select", "WHISPER", nil, name)
	else
		if(ITEMINDEX  == 1)then 
			ITEMINDEX = #QUESTREWARDTABLE
			bwSelectQuest(QUESTREWARDTABLE[ITEMINDEX])
		elseif(ITEMINDEX > 1)then 
			ITEMINDEX = ITEMINDEX - 1
			bwSelectQuest(QUESTREWARDTABLE[ITEMINDEX])
		end 
	end
end--]]

function bwRewardUp()				--cycle through quest rewards, replace constant with selection
	local numChoices = GetNumQuestLogChoices()
	local name, texture, numItems, quality, isUsable
		if(tonumber(numChoices) == 0 or tonumber(numChoices) == 1)then
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("No choices to choose.", "WHISPER", nil, name)
		else
			if(tonumber(numChoices) > REWARDSELECT)then
				REWARDSELECT = REWARDSELECT + 1
				name, texture, numItems, quality, isUsable = GetQuestLogChoiceInfo(REWARDSELECT)
				JocysCom_SendChatMessageStop(2);
				SendChatMessage("Reward Selected: " ..name, "WHISPER", nil, name)
			elseif(tonumber(numChoices) == REWARDSELECT)then 
				REWARDSELECT = 1
				name, texture, numItems, quality, isUsable = GetQuestLogChoiceInfo(REWARDSELECT)
				JocysCom_SendChatMessageStop(2);
				SendChatMessage("Reward Selected: " ..name, "WHISPER", nil, name)
			end
		end
end

function bwRewardDown()  		--cycle through quest rewards, replace constant with selection
	local numChoices = GetNumQuestLogChoices()
	local name, texture, numItems, quality, isUsable
		if(tonumber(numChoices) == 0 or tonumber(numChoices) == 1)then
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("No choices to choose.", "WHISPER", nil, name)
		else
			if(REWARDSELECT > 1)then
				REWARDSELECT = REWARDSELECT - 1
				name, texture, numItems, quality, isUsable = GetQuestLogChoiceInfo(REWARDSELECT)
				JocysCom_SendChatMessageStop(2);
				SendChatMessage("Reward Selected: " ..name, "WHISPER", nil, name)
			elseif(REWARDSELECT == 1)then 
				REWARDSELECT = numChoices
				name, texture, numItems, quality, isUsable = GetQuestLogChoiceInfo(REWARDSELECT)
				JocysCom_SendChatMessageStop(2);
				SendChatMessage("Reward Selected: " ..name, "WHISPER", nil, name)
			end
		end
end 

function bwAcceptQuest()
	AcceptQuest()
	bwAcceptOrDeclineMode = false 
end

function bwDeclineQuest() 
	DeclineQuest()
	bwAcceptOrDeclineMode = false 
end 

function bwQuestComplete()
	local numChoices = GetNumQuestLogChoices()
	local cIndex 
	local rewardChoice
	JocysCom_SendChatMessageStop(2);
	SendChatMessage("Completing....", "WHISPER", nil, name)
	if(tonumber(numChoices) == 1)then
		GetQuestReward(1)
	elseif(tonumber(numChoices) > 1)then
		GetQuestReward(REWARDSELECT)
	else
		GetQuestReward(nil) 
	--	for i=1, #QUESTREWARDTABLE do
	--		if(QUESTREWARDTABLE[i] == selectedQuest)then
	--			cIndex = i 
	--		end 
	--	end 
--		rewardChoice = REWARDSELECT[cIndex] 
	--	GetQuestReward(rewardChoice)
	end
	
	BWQUESTCOMPLETEMODE = false
	REWARDSELECT = 1
end 