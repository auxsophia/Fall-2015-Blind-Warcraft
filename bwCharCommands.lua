--Module for getting information about your character
--This includes things like currently equipped items,
--various statistics, and experience and reputation.

function bwCharModeActivate(argument)

	if(BWCHARMODE == false) then
	
	bwFlagSwitch("character")
		
	EQUIPMENT[0] = GetInventoryItemID("player", GetInventorySlotInfo("BackSlot"))
	EQUIPMENT[1] = GetInventoryItemID("player", GetInventorySlotInfo("ChestSlot"))
	EQUIPMENT[2] = GetInventoryItemID("player", GetInventorySlotInfo("FeetSlot"))
	EQUIPMENT[3] = GetInventoryItemID("player", GetInventorySlotInfo("Finger0Slot"))
	EQUIPMENT[4] = GetInventoryItemID("player", GetInventorySlotInfo("Finger1Slot"))
	EQUIPMENT[5] = GetInventoryItemID("player", GetInventorySlotInfo("HandsSlot"))
	EQUIPMENT[6] = GetInventoryItemID("player", GetInventorySlotInfo("HeadSlot"))
	EQUIPMENT[7] = GetInventoryItemID("player", GetInventorySlotInfo("LegsSlot"))
	EQUIPMENT[8] = GetInventoryItemID("player", GetInventorySlotInfo("MainHandSlot"))
	EQUIPMENT[9] = GetInventoryItemID("player", GetInventorySlotInfo("NeckSlot"))
	--EQUIPMENT[10] = GetInventoryItemID("player", GetInventorySlotInfo("RangedSlot"))
	EQUIPMENT[11] = GetInventoryItemID("player", GetInventorySlotInfo("SecondaryHandSlot"))
	EQUIPMENT[12] = GetInventoryItemID("player", GetInventorySlotInfo("ShirtSlot"))
	EQUIPMENT[13] = GetInventoryItemID("player", GetInventorySlotInfo("ShoulderSlot"))
	EQUIPMENT[14] = GetInventoryItemID("player", GetInventorySlotInfo("TabardSlot"))
	EQUIPMENT[15] = GetInventoryItemID("player", GetInventorySlotInfo("Trinket0Slot"))
	EQUIPMENT[16] = GetInventoryItemID("player", GetInventorySlotInfo("Trinket1Slot"))
	EQUIPMENT[17] = GetInventoryItemID("player", GetInventorySlotInfo("WaistSlot"))
	EQUIPMENT[18] = GetInventoryItemID("player", GetInventorySlotInfo("WristSlot"))

	SendChatMessage("Character Mode Activated." , WHISPER, nil, NAME)

	
	
	else
	
	BWCHARMODE = false
	SendChatMessage("Character Mode Deactivated.", WHISPER, nil, NAME)
	end
end

function bwCharNext()

	if(BWCHARINDEX == 9) then
		BWCHARINDEX = BWCHARINDEX+2
		bwCharGetInfo(EQUIPMENT[BWCHARINDEX], "class")
	else
		if(BWCHARINDEX < 18) then
			BWCHARINDEX = BWCHARINDEX+1
			bwCharGetInfo(EQUIPMENT[BWCHARINDEX], "class")
		else
			SendChatMessage("Bottom of character panel", WHISPER, nil, NAME)
		end
	end
end

function bwCharPrev()

	if(BWCHARINDEX == 11) then
		BWCHARINDEX = BWCHARINDEX-2
		bwCharGetInfo(EQUIPMENT[BWCHARINDEX], "class")
	else
		if(BWCHARINDEX>0) then
			BWCHARINDEX = BWCHARINDEX-1
			bwCharGetInfo(EQUIPMENT[BWCHARINDEX], "class")
		else
			SendChatMessage("Top of character panel", WHISPER, nil, NAME)
		end
	end
end

function bwCharGetInfo(argument, code)

	print("running getInfo function")
	local itemName, iLevel, armorclass, subClass, equipSlot
	if(tonumber(argument)~=nil and code == "") then
		--print("argument is a number and code is nil")
		--if our argument is a number
		itemName, ph, ph, iLevel, ph, armorclass, subClass, ph, equipSlot, ph, ph = GetItemInfo(argument)
		SendChatMessage(itemName .." ".. subClass, WHISPER, nil, NAME)
	
	elseif(tonumber(argument)~=nil and code == "class") then
		--print("argument is a number and code is not nil")
		SendChatMessage(SLOTNAMES[BWCHARINDEX], WHISPER, nil, NAME)
		--if its one of the other arguments
	elseif(argument == nil and code == "class") then
		--print("argument is nil and code is not nil")
		SendChatMessage(SLOTNAMES[BWCHARINDEX], WHISPER, nil, NAME)
	else
		--print("error in arguments")

	end

end

function bwCharTestFunc(argument)

	local testvar
	EQUIPMENT[1] = GetInventoryItemID("player", GetInventorySlotInfo("ChestSlot"))
	print(EQUIPMENT[1])
	print(GetItemInfo(EQUIPMENT[1]))

end