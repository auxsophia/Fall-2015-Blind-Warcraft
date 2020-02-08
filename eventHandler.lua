local frame, events = CreateFrame("Frame"), {};

name = GetUnitName("player", 1)

function events:PLAYER_ENTERING_WORLD(...)
	SendChatMessage("Welcome to Wow, "..name.."!", WHISPER, nil, name) --Welcome message upon login
	SendChatMessage("Type /bw help if you need a list of commands.", WHISPER, nil, name)
	
	if(UnitLevel(NAME) == 1 and NEWCHAR == true) then
		newCharacter()
		NEWCHAR = false
	end
		
	--	Experimental function to set bindings for new characters
--	if(NEWCHAR == nil)then
--		SendChatMessage("Some of your key bindings have changed with the new character" , WHISPER, nil, name)
--		newCharacter()
--	end
	--Testing, will add this to a function soon to clean it up.
	--[[if(RLOADED == false)then 					--If this is your first time loading this addon
		local numEntries, numQuests = GetNumQuestLogEntries()
		if(tonumber(numQuests) >= 1)then	--And you have quests in your log
			for i=1,tonumber(numQuests) do  --For each quest 
				SelectQuestLogEntry(i)
				local numChoices = GetNumQuestLogChoices()		 
				if(numChoices > 1)then				--If the quest has multiple rewards
					QUESTREWARDTABLE[#QUESTREWARDTABLE + 1] = i		--Add it to the rewards table 
					REWARDSELECT[#REWARDSELECT + 1] = 1 
					local questTitle, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(i)		--Output which quest has been put in the table 
					SendChatMessage("Quest number " ..i.. ": " .. questTitle .. "Has multiple rewards to select from.  Please enter reward command to pick your reward", "WHISPER", nil, name)
				end
			end
		end
		RLOADED = true;			--Only do this when the addon is loaded for the first time, this variable is saved.
	end --]]

end

function events:PLAYER_LEAVING_WORLD(...)

--nothing here right now

end

function events:PLAYER_LEVEL_UP(...)


	SendChatMessage("You have reached level " .. UnitLevel(name) .. "!", WHISPER, nil, name) --Message upon level up

end

function events:PLAYER_REGEN_DISABLED(...)
	
	JocysCom_SendChatMessageStop(2);	
	SendChatMessage("Entering Combat", WHISPER, nil, name)
	INCOMBAT = true
	
end

function events:PLAYER_REGEN_ENABLED(...)

	HEALTHCHECK75 = false
	HEALTHCHECK50 = false
	HEALTHCHECK25 = false
	INCOMBAT = false
	targetChanged = true

end

function events:UNIT_HEALTH(unitID)

	if(unitID == "player") then
		CURRHEALTHPERCENT = UnitHealth("player")/UnitHealthMax("player") --calculate the current health percentage of the player
	end
	
	if(CURRHEALTHPERCENT >= 0.75) then --reset some flags if need be
		HEALTHCHECK75 = false
		HEALTHCHECK50 = false
		HEALTHCHECK25 = false
	elseif(CURRHEALTHPERCENT >= 0.5) then
		HEALTHCHECK50 = false
		HEALTHCHECK25 = false
	elseif(CURRHEALTHPERCENT >= 0.25) then
		HEALTHCHECK25 = false
	end
	if(CURRHEALTHPERCENT == 1 and INCOMBAT == false)then
		--SendChatMessage("Health is full", WHISPER, nil, name) --need some indication that they're at full health
	end
end

function events:UNIT_COMBAT(unitID, action, descriptor, damage, damageType)
	
	--We can discuss the actual format of the combat messages because I'm not sure what would be the best way to put them.
	
	--[[print(unitID)
	print(action)
	print(descriptor)
	print(damage)]]--
	if(UnitAffectingCombat("player") == true) then --If the player is in combat
	
		if(math.floor(CURRHEALTHPERCENT*100) <= 75 and math.floor(CURRHEALTHPERCENT*100) > 50 and HEALTHCHECK75 == false) then
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("Health three fourths", WHISPER, nil, name)
			HEALTHCHECK75 = true
		elseif(math.floor(CURRHEALTHPERCENT*100) <= 50 and math.floor(CURRHEALTHPERCENT*100) > 25 and HEALTHCHECK50 == false) then
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("Health one half", WHISPER, nil, name)
			HEALTHCHECK50 = true
		elseif(math.floor(CURRHEALTHPERCENT*100) <= 25 and HEALTHCHECK25 == false) then
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("Health one fourth", WHISPER, nil, name)
			HEALTHCHECK25 = true
		end
	
		if(unitID == "player") then
			if(action =="HEAL") then
				JocysCom_SendChatMessageStop(2);
				SendChatMessage("Healed to " .. math.floor(CURRHEALTHPERCENT*100) .. "%.", WHISPER, nil, name) --wanna reset some of those flags in case the player gets healed past them
			end
		end
	
	--[[if(unitID == "player") then --and the player takes some damage/healing
		if(action == "WOUND") then
			if(descriptor == "HIT" or descriptor == "") then
				SendChatMessage("Took " .. damage .. ".", WHISPER, nil, name)
			elseif(descriptor == "CRIT") then
				SendChatMessage("Took " .. damage .. ".", WHISPER, nil, name)
			elseif(descriptor == "CRUSHING") then
				SendChatMessage("Took " .. damage .. ".", WHISPER, nil, name)
			end
		elseif(action == "DODGE") then
			SendChatMessage("Dodged", WHISPER, nil, name)
		elseif(action == "HEAL") then
			SendChatMessage("Healed " .. damage .. ".", WHISPER, nil, name)
		end
	end]]--
	
	if(unitID == "target") then --and the player's target takes damage/healing
		if(action == "WOUND") then
			--[[if(descriptor == "HIT" or descriptor == "") then
				SendChatMessage("Hit " .. damage .. ".", WHISPER, nil, name)
			else]]if(descriptor == "CRIT") then
				JocysCom_SendChatMessageStop(2);
				SendChatMessage("Crit " .. damage .. ".", WHISPER, nil, name)
			elseif(descriptor == "CRUSHING") then
				JocysCom_SendChatMessageStop(2);
				SendChatMessage("Crushing blow " .. damage .. ".", WHISPER, nil, name)
			end
		elseif(action == "DODGE") then
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("Enemy Dodged", WHISPER, nil, name)
		elseif(action == "HEAL") then
			JocysCom_SendChatMessageStop(2);
			SendChatMessage("Enemy Healed " .. damage .. ".", WHISPER, nil, name)
		end
	end
	
	end

end

function events: UNIT_QUEST_LOG_CHANGED(unitID)
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
		SendChatMessage("Please select a quest with the following command. Forward slash. B. W. Quest. Number.", "WHISPER", nil, name)		--error message
	end
end 

function events: QUEST_ACCEPTED(questIndex, questID)
	--SelectQuestLogEntry(questIndex)
	--local numChoices = GetNumQuestLogChoices()
	--if(numChoices > 1)then
		--	QUESTREWARDTABLE[#QUESTREWARDTABLE + 1] = questIndex
			--REWARDSELECT[#REWARDSELECT + 1] = 1 
	--end
end

function events: QUEST_COMPLETE(...)
	local numChoices = GetNumQuestLogChoices()
	if(tonumber(numChoices) > 1)then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("Up and down arrows to select a reward.", "WHISPER", nil, name)
	end
	
	BWQUESTCOMPLETEMODE = true 
	bwAcceptOrDeclineMode = false  
	bwRewardMode = false 
	bwQuestMode = false
	bwInvMode = false
	bwSBMode = false 
end 

function events: QUEST_DETAIL(...)
	bwAcceptOrDeclineMode = true 
	bwRewardMode = false 
	bwQuestMode = false
	bwInvMode = false
	bwSBMode = false 
	BWQUESTCOMPLETEMODE = false 
end 

function events: GOSSIP_SHOW(...)
	num = GetNumGossipAvailableQuests()
	
	if(num==1)then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage(num.." quest available.", "WHISPER", nil, name)
	else
		JocysCom_SendChatMessageStop(2);
		SendChatMessage(num.." quests available.", "WHISPER", nil, name)
	
	SelectGossipAvailableQuest(tonumber(num))
	end
end

function events:UNIT_TARGET(unitID)
	--print("in unit target event")
	if(unitID == "player") then
		TARGET = UnitGUID("target")
		if(TARGET ~= LASTTARGET and TARGET ~= nil) then	
			--print("Targeted " .. GetUnitName("target"))
			LASTTARGET = TARGET
			JocysCom_SendChatMessageStop(2);	
			SendChatMessage(GetUnitName("target") .. " Level " .. UnitLevel("target") , WHISPER, nil, name)
			targetChanged = false
		else
			--print("Lost target")
			JocysCom_SendChatMessageStop(2);	
			SendChatMessage("Lost target", WHISPER, nil, name)
			targetChanged = true
		end
	end
end

frame:SetScript("OnEvent", function(self, event, ...)
 events[event](self, ...); -- call one of the functions above
end);

for k, v in pairs(events) do
 frame:RegisterEvent(k); -- Register all events for which handlers have been defined
end
