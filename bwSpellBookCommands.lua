
--activate the spellbook mode
function bwSBActivate()

	if(BWSBMODE == false) then
		bwFlagSwitch("spellbook")
		if(bwSBModePrevious == false) then
			ph, ph, CURRSBINDEX, MAXSBINDEX = GetSpellTabInfo(2) --get the offset of the class spell tab
			SBOFFSET = CURRSBINDEX
			MAXSBINDEX = CURRSBINDEX+MAXSBINDEX+2 --without the plus 2 your spellbook i/s 2 spells shorter
			CURRSBINDEX = CURRSBINDEX+1
			bwSBModePrevious = true
		end
		JocysCom_SendChatMessageStop(2)
		SendChatMessage("Spellbook mode activated.", WHISPER, nil, NAME)
		bwSBGetInfo()
		bwSBGiveInfo()
		--print("SB mode set to true")
	else
		BWSBMODE = false
		--print("SB mode set to false")
	end
	bwSBGetInfo()
end

function bwSBNavigateNext()

	if(CURRSBINDEX == (MAXSBINDEX -2)) then 
		bwError("indexhigh")
	else
		CURRSBINDEX = CURRSBINDEX+1
		bwSBGetInfo()
		bwSBGiveInfo(false)
	end
end

function bwSBNavigatePrev()

	if(CURRSBINDEX == (SBOFFSET+1)) then
		bwError("indexlow")
	else
		CURRSBINDEX = CURRSBINDEX-1
		bwSBGetInfo()
		bwSBGiveInfo(false)
	end
end



function bwSBGetInfo()

	SPELLNAME, ph, ph, SPELLCASTTIME, SPELLMINRANGE, SPELLMAXRANGE, SPELLID = GetSpellInfo(CURRSBINDEX, "spell")
	SPELLDESC = GetSpellDescription(SPELLID)
end

function bwSBGiveInfo(full)

--[[Some spell descriptions gotten with GetSpellDescription contain characters that the SendChatMessage function can't handle.
	In this case the character was "|".  We should probably check all the classes so that we can check all of the spell descriptions for these invalid characters.
]]--
	local lineI, lineJ
	if(full == true) then --if they want specific casting info
		lineI, lineJ = string.find(SPELLDESC, "|") 	--used to parse out things not accepted by the chat message function ie "|"
													--string.find finds the first instance and the last instance of the argument string within a string. look it up
		if(lineI ~= nil) then
			SPELLDESC = strsub(SPELLDESC, 1, lineI-2) --actually get rid of the bad character
			JocysCom_SendChatMessageStop(2)
			SendChatMessage(SPELLNAME .. " learned at level " .. GetSpellLevelLearned(SPELLID) .. " " .. SPELLDESC .." hour.", "WHISPER", nil, NAME)
		else
			JocysCom_SendChatMessageStop(2)
			SendChatMessage(SPELLNAME .. " learned at level " .. GetSpellLevelLearned(SPELLID) .. " " .. SPELLDESC .." ", "WHISPER", nil, NAME)
		end
		if(SPELLMINRANGE == 0 and SPELLMAXRANGE == 0 and SPELLCASTTIME/1000 == 0) then
		JocysCom_SendChatMessageStop(2)
		SendChatMessage("It is instant cast, and you must be within melee range.", WHISPER, nil, NAME)
		elseif(SPELLMINRANGE == 0 and SPELLMAXRANGE == 0 and SPELLCASTTIME/1000 ~= 0) then
		JocysCom_SendChatMessageStop(2)
		SendChatMessage("It has a casting time of " .. SPELLCASTTIME/1000 .. " seconds, and you must be within melee range", WHISPER, nil, NAME)
		else
		JocysCom_SendChatMessageStop(2)
		SendChatMessage("It has a casting time of " .. SPELLCASTTIME/1000 .. " seconds, a minimum range of " .. SPELLMINRANGE .. " yards and a maximum range of " .. SPELLMAXRANGE .. " yards", WHISPER, nil, NAME)
		end
	else
		JocysCom_SendChatMessageStop(2)
		SendChatMessage(SPELLNAME, "WHISPER", nil, NAME)
	end

end

function bwSBMoreInfo()
	bwSBGetInfo()
	bwSBGiveInfo(true)
end

function bwSBBind(args)

	local playerClass = UnitClass("player")
	
	PickupSpell(SPELLID)
	args = tonumber(args)
	if(CursorHasSpell()) then
		----print("going to place spell on " .. args)
		--print(UnitLevel("player"))
		--print(GetSpellLevelLearned(SPELLID))
		if((playerClass == "Monk" or playerClass == "Warrior" or playerClass == "Druid") and (UnitLevel("player") >= GetSpellLevelLearned(SPELLID))) then
			PlaceAction(args+72)
			--PickupAction(tonumber(args))
		elseif((UnitLevel("player") >= GetSpellLevelLearned(SPELLID))) then
			PlaceAction(tonumber(args))
		else
			JocysCom_SendChatMessageStop(2)
			SendChatMessage("You are not high enough level to use this spell.", WHISPER, nil, NAME)
		end
	end
	
	ClearCursor()
	----print("This is where the current spell should be bound to the specified key")
	--print("In this case it would be " .. SPELLNAME .. " bound to " .. args)
end

function bwSBBindInfo(args)
	local playerClass = UnitClass("player")
	local spellType, id, subType, spell, NAMEOfSpell 
	if(playerClass == "Monk" or playerClass == "Warrior" or playerClass == "Druid") then
			spellType, id, subType, spell = GetActionInfo(tonumber(args)+72)
			
		else
			spellType, id, subType, spell = GetActionInfo(tonumber(args))
		end
	NAMEOfSpell = GetSpellInfo(id)
	--print("Giving info for the spell in slot " .. args)
	--print(spellType)
	--print(id)
	--print(subType)
	--print(NAMEOfSpell)
end

