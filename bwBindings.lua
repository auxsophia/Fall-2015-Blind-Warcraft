
--[[If you want to add more bindings refer to this guide
	the format for bindings in this file should be
	
	BINDING_NAME_name of your binding = "Thing that will be displayed in the bindings interface panel"
	
	the format for bindings in the xml file should be
	
	<Binding name="name of your binding" header="BLIND_WARCRAFT">
		function for your binding to call
	</Binding>
	
	The "name of your binding" parts should match in the xml and this lua file.
	]]--
	
BINDING_HEADER_BLIND_WARCRAFT = "Blind Warcraft"
BINDING_NAME_BINDING_TEST1 = "Test binding #1"
BINDING_NAME_BINDING_TEST2 = "Test binding #2"
BINDING_NAME_BW_NAVIGATE_NEXT = "Navigate to the next spell/item/quest"
BINDING_NAME_BW_NAVIGATE_PREV = "Navigate to the previous spell/item/quest"
BINDING_NAME_BW_NAVIGATE_UP = "Navigate to the next reward"
BINDING_NAME_BW_NAVIGATE_DOWN = "Navigate to the previous reward"
BINDING_NAME_BW_STOPREADING = "Stop the text to speech"
BINDING_NAME_BW_MOREINFO = "Give more information about a spell/item"
BINDING_NAME_BW_QUESTDIR = "Direction of current quest"
BINDING_NAME_BW_CALCDISP = "Calculates if there is displacement"
BINDING_NAME_BW_SELECTEDQUEST = "Return title of selected quest"
BINDING_NAME_BW_SELECTEDQUEST_PROGRESS = "Return quest progress"

function BindingTest_Test1()
	--print("Test binding #1 activated")
end

function BindingTest_Test2(keystate)
	if keystate == "down" then
		--print("Test binding #2 pressed")
	else
		--print("Test binding #2 released")
	end
end

function bwInvNavigateNext()
	----print("bwInvNavigateNext()")
end

function bwInvNavigatePrev()
	----print("bwInvNavigatePrev()")
end

function bwInvMoreInfo()
	--print("bwInvMoreInfo()")
end

function bwSetBindings(args)
	local keybind, action = string.match(args, "(.*) (%w*)" )
	local bindWorks

--	--print(args)
--	--print(keybind)
--	--print(action)
	
--  Must use ALL CAPS if doing a multikey bind (IE - SHIFT-q or SHIFT-Q) otherwise you get unknown keybind command
	if (keybind == nil or action == nil) then
		SendChatMessage("The command line entry to bind a key is /bw set binding <keybind> <action>", "WHISPER", nil, name)
	else
		bindWorks = SetBinding(keybind,action)
	end
--	--print(bindWorks)
	if (bindWorks == true) then
		--print("Keybind succesful. You bound " .. keybind .. " to " .. action .. ".")
	else
		--print("Unknown keybind command. The input for a keybind is <keybind> <action>.")
	end

end

--If character level = 1 (IE new character), will set bindings for the player.
function newCharacter()
	local level= UnitLevel(NAME)
--	--print(UnitLevel(name))
--	--print(level < 2)
	if(level == 1) then
	SendChatMessage("A few of your key bindings have changed." , WHISPER, nil, NAME)
	SetBinding("'", "BW_SELECTEDQUEST")
	SetBinding(".","BW_STOPREADING")
	SetBinding("SHIFT-/","BW_SELECTEDQUEST_PROGRESS")
	SetBinding("SHIFT-uarr", "BW_NAVIGATE_NEXT")
	SetBinding("SHIFT-darr", "BW_NAVIGATE_PREVIOUS")
	SetBinding("SHIFT-rarr", "BW_MOREINFO")
	
	end
	NEWCHAR = false
end