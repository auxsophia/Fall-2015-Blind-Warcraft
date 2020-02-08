-- Gives descriptions based on arg chosen
function bwHelp(command, args, args2)
--	print(command)
--	print(args)
--	print(args2)
	if (args == "") and (bwQuestMode == false) and (BWSBMODE == false) and (BWBAGMODE == false) and (BWCHARMODE == false) then
	--	SendChatMessage("Type /bw help, quest, spellbook, bag, or read for more information", "WHISPER", nil, name)
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("Type /bw help, spellbook, quest, read, or bag for more information", "WHISPER", nil, name)
	elseif(string.match(args, "bindings") == "bindings") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The set bindings command allows you to set a binding via the chat line", "WHISPER", nil , name)
		SendChatMessage("The command is /bw set bindings key action", "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "count" or args == "count") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The /bw quest count command returns the number of quests you have in your quest log", "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "number" or args == "number") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The /bw  quest number stores the index of the given quest and returns  the title of the quest", "WHISPER", nil , name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "stored" or args == "stored") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The /bw quest stored command returns the title of your currently stored quest", "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "abandon" or args == "abandon") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("the /bw quest abandon command will abandon your currently selected quest", "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "progress" or rags == "progress") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The /bw quest progress will inform you of your progress on the saved quest", "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "all" or args == "all") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw quest all command gives the titles and indexes of all the quests in your quest log.", "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "location" or args == "location") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw quest locate command will help you locate the general area of a quest.", "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "all" or args == "all") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw quest all command will return the indices and titles of all your quests.", "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "select" or args == "select") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The /bw quest select command will allow you to select one of your stored quests." , "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "reward" or args == "reward") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The /bw quest reward command will help you to select a quest reward.", "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) and (args2 == "select" or args == "select") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The /bw quest select command will allow you to cycle through your quests in your quest log.", "WHISPER", nil, name)
	elseif(args == "quest" or bwQuestMode == true) then
		JocysCom_SendChatMessageStop(2);
	--  SendChatMessage("Type forward slash b w space help space quest count, ")
		SendChatMessage("The following quest commands are: count, number, stored, abandon, progress, objective", "WHISPER", nil , name)
	elseif(args == "sb" or args == "spellbook"  or BWSBMODE == true) and (args2 == "next" or args2 == "nav next" or args2 == "navigate next" or args == "next" or args == "nav next" or args == "navigate next") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw spellbook next command moves to the next spellbook page", "WHISPER", nil, name)
	elseif(args == "spellbook" or args == "sb"or BWSBMODE == true) and (args2 == "prev" or args2 == "nav prev" or args2 == "navigate prev" or args == "prev" or args == "nav prev" or args == "navigate prev") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw spellbook previous command moves to the previous spellbook page", "WHISPER", nil, name)
	elseif(args == "spellbook" or args == "sb" or BWSBMODE == true) and (args2 == "more" or args2 == "info" or args2 == "more info" or args == "info" or args == "more" or args == "more info") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw spellbook more command gives more information on a spell or ability","WHISPER", nil, name)
	elseif(args == "spellbook" or args == "sb" or BWSBMODE == true) and (args2 == "bind" or args == "bind") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw spellbook bind command will bind an ability the character has learned to a keybind.", "WHISPER", nil, name)
	elseif(args == "spellbook" or args == "sb" or BWSBMODE == true) and (args2 == "bindinfo" or args == "bindinfo") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw spellbook bindinfo command will return information on an ability bound to a keybind.", "WHISPER", nil, name)
	elseif(args == "spellbook" or args == "sb" or BWSBMODE == true) then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw following spellbook commands are: next, previous, or more", "WHISPER", nil, name)
	elseif(args == "bag" or BWBAGMODE == true) and (args2 == "activate" or args == "activate" or args == nil) then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw bag activate command will return the number of items in your bags and activate bag mode.", "WHISPER", nil, name)
	elseif(args == "bag" or BWBAGMODE == true) and (args2 == "next" or args == "next") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw bag next command will return the name of an item in the next bag slot.", "WHISPER", nil, name)
	elseif (args == "bag" or BWBAGMODE == true) and (args2 == "prev" or args2 == "previous" or args == "previous" or args == "prev" ) then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The /bw bag previous command will return the name of an item in the previous bag slot.", "WHISPER", nil, name)
	elseif(args == "bag" or BWBAGMODE == true) and (args2 == "info" or args == "info") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The /bw bag info command will return the the name of your currently item selected.", "WHISPER", nil, name)
	elseif(args == "bag" or BWBAGMODE == true) and (args2 == "destroy" or args == "destroy") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw bag destroy command will destroy the currently selected item." , "WHISPER", nil, name)
	elseif(args == "bag" or BWBAGMODE == true) and (args2 == "equip" or args == "equip") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw bag equip command will equip the selected item if possible." ,"WHISPER", nil, name)
	elseif(args == "bag" or BWBAGMODE == true) and (args2 == "bind" or args == "bind") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw bag bind <bindkey> will bind the current item selected into the requested slot.", "WHISPER", nil, name)
	elseif(args == "bag" or BWBAGMODE == true) and (args2 == "moreinfo" or args2 == "more info" or args == "moreinfo" or args == "more info") then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The /bw bag moreinfo command will return an in depth description of the item." ,"WHISPER", nil, name)
	elseif(args == "bag" or BWBAGMODE == true) and (args2 == "number" or args == "number") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The /bw bag number command will attempt to give the name of an item at the given index.", "WHISPER", nil, name)
	elseif(args == "bag" or BWBAGMODE == true) then
		JocysCom_SendChatMessageStop(2);	
		SendChatMessage("The following /bw bag commands are: activate, next, prev, info, or number", "WHISPER", nil, name)
	elseif(args == "read") then
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("The read commands are ...", "WHISPER", nil, name)
	end
end
