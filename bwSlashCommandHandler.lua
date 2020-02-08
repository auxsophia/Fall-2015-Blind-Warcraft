--Slash Command Handler


function bwSlashCommandHandler(msg, editbox)

    local command, args = msg:match("^(%S*)%s*(.-)$") -- parse out the command and arguments.  "/blindwow help bagscan"  help->command, bagscan->args
	local args, args2 = args:match("^(%S*)%s*(.-)$") -- this is to parse out any sub arguments for commands.

	if(args2 ~=nil) then
	--print("Got command " .. command .. " with arguments " .. args .. " and args2 = " .. args2)
	elseif(args ~= nil) then
	--print("Got command " .. command .. " with arguments " .. args)
	else
	--print("Got command " .. command .. " with no arguments")
	end

	if(command == "help") then -- /bw help will describe the available options
		bwHelp(command, args, args2)
	elseif(command == "bag" and args == "" and BWBAGMODE == false) then -- /bw bag initializes functionality and speaks the name of the first item.
		bwBagActivate()
		bwBagInfo()
	elseif(command == "bag" and args == "" and BWBAGMODE == true) then
		SendChatMessage("Bag mode deactivated.", WHISPER, nil, name)
		BWBAGMODE = false
	elseif(((command == "bag" and args == "next") or command == "next") and BWBAGMODE == true) then
		bwBagNext()
	elseif(((command == "bag" and args == "prev") or command == "prev" or command == "previous") and BWBAGMODE == true) then
		bwBagPrev()
	elseif(((command == "bag" and (args == "moreinfo" or (args == "more" and args2 == "info"))) or 
		(command == "info" or command == "more" or command == "moreinfo")) and BWBAGMODE == true) then
		bwBagMoreInfo()
	elseif(command == "bag" and args == "bind" and tonumber(args2) ~= nil and BWBAGMODE == true) then
		bwBagBindItem(tonumber(args2))
	elseif(command == "bind" and tonumber(args) ~= nil and BWBAGMODE == true) then
		bwBagBindItem(tonumber(args))
	elseif(command == "bag" and args == "item" and tonumber(args2) ~= nil and BWBAGMODE == true) then
		bwBagItemAtIndex(tonumber(args2))
	elseif(command == "item" and tonumber(args) ~= nil and BWBAGMODE == true) then
		bwBagItemAtIndex(tonumber(args))
	elseif(((command == "bag" and args == "equip") or command == "equip") and BWBAGMODE == true) then
		bwBagEquip()
		BWBAGMODE = false
		bwBagActivate()
		bwBagInfo()
	elseif(((command == "bag" and args == "destroy") or command == "destroy") and BWBAGMODE == true) then
		bwBagDelete()
		BWBAGMODE = false
		bwBagActivate()
		bwBagInfo()
	elseif(command == "set" and string.match(args, "binding") == "binding") then
		bwSetBindings(args2)
	elseif(command == "quest" and args == "count") then -- /bw quest count tells you how many quests you have
		bwReturnQuestCount()
	elseif(command == "quest" and tonumber(args) ~= nil)then		--/bw quest # stores the index of the quest into the global variable and returns title of the quest
		bwSelectQuest(args)
	elseif(command == "quest" and args == "stored")then		--/bw quest stored returns the title of your currently stored quest
		bwReadStored()
	elseif(command == "quest" and args == "abandon")then 	--/bw quest abandon will abandon currently selected quest 
		bwAbandonQuest()
	elseif(command == "quest" and args == "progress")then 		--/bw quest progress show quest progress text
		bwQuestProgress()
	elseif(command == "quest" and args == "objective")then		--/bw quest objective is just to test getdistancesq() function, does nothing
		bwLocateObjective()
	elseif(command == "quest" and args == "all")then					--/bw quest all returns title and index of all quests
		bwReadAll()
	elseif(command == "quest" and args == "select")then
		bwCycleActivate()
	elseif(command == "quest" and args == "reward")then
		bwSelectReward() 
	elseif((command == "spellbook" or command == "sb") and (args == "") and BWSBMODE == false) then			--/bw sb or /bw spellbook activates the spellbook navigation mode
		bwSBActivate()
	elseif((command == "spellbook" or command == "sb") and (args == "") and BWSBMODE == true) then
		bwSBActivate()
	elseif(((command == "spellbook" or command == "sb") and (args == "next" or (args == "nav" and args2 == "next") or (args == "navigate" and args2 == "next")) or (command == "next") and BWSBMODE == true)) then
		bwSBNavigateNext()
	elseif(((command == "spellbook" or command == "sb") and (args == "prev" or (args == "nav" and args2 == "prev") or (args == "navigate" and args2 == "prev")) or command == "prev" and BWSBMODE == true)) then
		bwSBNavigatePrev()
	elseif(((command == "spellbook" or command == "sb") and (args =="more" or (args == "more" and args2 == "info") or (args == "moreinfo")) or (command =="more" or command =="info" or command == "moreinfo") and BWSBMODE == true)) then
		bwSBMoreInfo()
	elseif(((command == "spellbook" or command == "sb") and (args =="bind")) and BWSBMODE == true) then
		bwSBBind(args2)
	elseif(((command == "bind" and BWSBMODE == true))) then
		bwSBBind(args)
	elseif(((command == "spellbook" or command == "sb") and (args =="bindinfo")) or (command == "bindinfo") and BWSBMODE == true) then
		--print("running bindinfo")
		bwSBBindInfo(args2)
	elseif(command == "read" and args == "again") then
		--Quest panel needs to be open for JocysCom_PlayButtonButton_OnClick()
		--ShowUIPanel(QuestLogPopupDetailFrame)
		
		JocysCom_PlayButtonButton_OnClick()
	elseif((command == "character" or command == "char") and (args == "") and BWCHARMODE == false) then
		bwCharModeActivate()
	elseif((command == "character" or command == "char") and (args == "") and BWCHARMODE == true) then
		bwCharModeActivate()
	elseif((command == "waypoint" or command == "wp") and (args == "add")) then
		bwWaypointAddWP()
	end
end

function bwLoadCommandList() --function to associate slash commands with our addon

    SlashCmdList["BLINDWARCRAFT"] = bwSlashCommandHandler -- add bw slash command handler to the list of commands
    SLASH_BLINDWARCRAFT1 = "/blindwow" --associate /blindwow with the command entry for BLINDWARCRAFT
    SLASH_BLINDWARCRAFT2 = "/bw" --associate /bw with the command entry for BLINDWARCRAFT

    --For actual commands like "/bw bags" we can parse out the command in the bwSlashCommandHandler so no need to put them here

end

function bwError(errorString) --Error function

	if(errorString=="navigate") then
		--print("Error - no navigation mode active")
	elseif(errorString == "indexlow") then
		--print("Error - you are at the beginning of the spellbook")
	elseif(errorString == "indexhigh") then
		--print("Error - you are at the end of the spellbook")
	elseif(errorString == "moreinfo") then
		--print("Error - cannot get info because no mode is active")
	end
end
 
