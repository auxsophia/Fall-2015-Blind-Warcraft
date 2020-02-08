--Jehoshua Josue
--Requirements: 2 hotkeys, Btn1 for calculating bearing for direction, Btn2 to determine if there was a displacement
--10/30/15 

--Abstract: press Btn1, walk forward for a bit, press Btn1, tap on strafe right button, press Btn1: calculate bearing
--if pressCount == 3 then Btn2 can say "Left, left, right, etc." until user is facing +/- 0.3 from the questBearing

--Btn2 is for finding out if you actually moved at all, and how far you are from your selected target location (if any)

qDist1 = 0
qDist2 = 0
qDist3 = 0

x1 = 0
x2 = 0
y1 = 0
y2 = 0

questBearing = 0	--contains the correct bearing the player should face the quest
pressCount = 0	--counts how many times Btn1 was pressed
rightOrLeft = 0 --'0' means target is on your left, '1' means it's on your right
baseCaseX = 0 --baseCaseX and baseCaseY will check if user moved while using the bearing algorithm
baseCaseY = 0
xPos = 0
yPos = 0

function resetVariables()
	qDist1 = 0
	qDist2 = 0
	qDist3 = 0
	x1 = 0
	x2 = 0
	y1 = 0
	y2 = 0
	questBearing = 0
	pressCount = 0
	rightOrLeft = 0
	pressCount = 0
end

function calcQuestBearing()
	pressCount = pressCount + 1
	xPos, yPos = GetPlayerMapPosition(name)
	xPos = xPos * 10
	yPos = yPos * 10
	
	
	local facing = GetPlayerFacing()
	local distance = GetDistanceSqToQuest(selectedQuest)
	
	--RESET all variables if user changes the selected quest
	--QUESTCHANGE is from bwQuestCommands.lua
	if QUESTCHANGE == true then
		resetVariables()
	end
	
	if pressCount == 3 then
		JocysCom_SendChatMessageStop(2);
		--SendChatMessage("Third press", "WHISPER", nil, name)		
	
		qDist3 = distance	--distance from quest after strafe right
		
		qDist1 = math.sqrt(qDist1)
		qDist2 = math.sqrt(qDist2)
		qDist3 = math.sqrt(qDist3)
		
		
		--3rd press means that the bearing has been calculated and
		--the user must turn right until facing the correct bearing
		--but if he moves, reset everything, he has to do the algorithm steps all over again
		baseCaseX, baseCaseY = GetPlayerMapPosition(name)
		baseCaseX  = baseCaseX * 10
		baseCaseY = baseCaseY * 10
		--print("baseX: "..baseCaseX.." baseY: "..baseCaseY.." xPos: "..xPos.." yPos: "..yPos)
		--DEBUG
		----print("qDist1: "..qDist1)
		----print("qDist3: "..qDist3)
		
		local sideA = distFormula(x1,y1,x2,y2)
		sideA = sideA * (40/0.4087)	--convert sideA to yards
		
		
		--calculate side AC with law of cosines
		local numerator = sideA^2 + qDist2^2 - qDist1^2		
		local denominator = 2*sideA*qDist2
		
		
		local angleC = math.acos(numerator/denominator)	--in radians
		
		local complementAngle = 3.14159 - angleC 	--180deg - angleC
		
		--depending on whether the quest is on your left or right, decide how to add/subtract angles
		if qDist3 > qDist2 then		--you got further, quest is on your left
			rightOrLeft = 0
			questBearing = facing + complementAngle
				--if questBearing greater than 2pi, subtract
			if questBearing > 6.28319 then
				questBearing = math.abs(6.28319 - questBearing)
			end
		else	--you got closer, quest is on your right
			rightOrLeft = 1
			questBearing = facing - complementAngle
		
				--if bearing is negative, subtract value from 2pi
			if questBearing < 0	then
				questBearing = 6.28319 + questBearing
			end
		
		end
	
	
		if questBearing >= 0 and questBearing <= 6.28319 then
			bearingAdjust(facing, questBearing, rightOrLeft)
			
		else
		
			--the following is for cases where user travels along a vector that goes towards the target location
			local undefined = math.acos(-1/0)	--will be used for comparing with undefined values
			if questBearing == undefined and qDist3 <= qDist1 then	--if undefined triangle and you got closer to the quest		
				SendChatMessage("Keep moving in this general direction.", "WHISPER", nil, name)
				distance = math.sqrt(distance)
				SendChatMessage("You are "..string.format ("%i", distance).." yards away from your target location.", "WHISPER", nil, name)
				--reset press counter
				pressCount = 0
				
			elseif questBearing == undefined and qDist3 > qDist1 then
				--user needs to turn around 180 degrees
				questBearing = facing + 3.14159
				if questBearing > 6.28319 then
					questBearing = math.abs(6.28319 - questBearing)
				end		
			
				bearingAdjust(facing, questBearing, rightOrLeft)
			end
		end
		
		
		--print("compAngle: "..complementAngle)	--DEBUG
		
		--print("facing: "..facing)	--DEBUG
		--print("+compAngle: "..facing + complementAngle)	--DEBUG
		--print("-compAngle: "..facing - complementAngle)	--DEBUG
		--print("QuestBearing: "..questBearing)
		
	elseif pressCount == 2 then
		x2 = xPos
		y2 = yPos
		qDist2 = distance
		SendChatMessage("Second press, move sideways some.", "WHISPER", nil, name)
		--print("facing: "..facing)	--DEBUG
	elseif pressCount == 1 then
		x1 = xPos
		y1 = yPos
		qDist1 = distance
		JocysCom_SendChatMessageStop(2);
		SendChatMessage("First press, move forward some.", "WHISPER", nil, name)
			print("qDist1: " .. qDist1)
		--print("facing: "..facing)	--DEBUG

	else
		--user will press button multiple times until he is facing the bearing by +/- 0.3
		bearingAdjust(facing, questBearing, rightOrLeft)
		
	end	
	
	
	
end

--function to be called to tell user to turn left/right based on current bearing and target bearing
function bearingAdjust(facing, questBearing, rightOrLeft)

	local bearingDiff = facing - questBearing
	local absDiff = math.abs(bearingDiff)
	
	
		--DEBUG
		--print("questBearing: "..questBearing)
		--print("facing: "..facing)
		--print("absDiff: "..absDiff)
	
	
	if absDiff <= 0.3 then
		--done adjusting bearing , tell user to move forward
		SendChatMessage("Bearing set. Move forward", "WHISPER", nil, name)
		
		--RESET all variables
		resetVariables()
	else --tell user to turn right, only if they haven't moved
	
		if  baseCaseX ~= xPos or baseCaseY ~= yPos then		--if user moved (user is only supposed to turn around)
			--RESET all variables
			resetVariables()
			SendChatMessage("You shouldn't move yet, direction was reset.", "WHISPER", nil, name)
		else
			SendChatMessage("Turn Right.", "WHISPER", nil, name)	
		
		
			if TARGETEXISTS == 1 then	--there is a target location, tell user how far it is from current location
				local distance = GetDistanceSqToQuest(selectedQuest)
				distance = math.sqrt(distance)
				
				if distance <= 10 then --tell user if they are within 10 yards of target location
					SendChatMessage("You are "..string.format ("%i", distance).." yards away from your target location.", "WHISPER", nil, name)
				end
			end
		end
	end

end



function distFormula(x1,y1,x2,y2)
	local distResult = math.sqrt((x2-x1)^2 + (y2-y1)^2)
	
	return distResult
end

--=========================================================================================
--global variables
TARGETEXISTS = 0
DISPX1 = 0
DISPX2 = 0
DISPY1 = 0
DISPY2 = 0
DISP_PRESSCOUNTER = 0


--press button 1st, attempt to move, press button 2nd time
--calcDisplacement will determine if you're even moving

--this function tells you if you moved and 
function calcDisplacement()

	DISP_PRESSCOUNTER = DISP_PRESSCOUNTER + 1

	if DISP_PRESSCOUNTER == 1 then
		DISPX1, DISPY1 = GetPlayerMapPosition(name)
		SendChatMessage("First location recorded", "WHISPER", nil, name)
	
	elseif DISP_PRESSCOUNTER == 2 then
		DISPX2, DISPY2 = GetPlayerMapPosition(name)

		local displacement = distFormula(DISPX1,DISPY1,DISPX2,DISPY2)
		
		if displacement > 0 then
			SendChatMessage("You have moved", "WHISPER", nil, name)
		else
			SendChatMessage("You did not move", "WHISPER", nil, name)
		end
		
		if TARGETEXISTS == 1 then	--there is a target location, tell user how far it is from current location
			local distance = GetDistanceSqToQuest(selectedQuest)
			distance = math.sqrt(distance)
			
			SendChatMessage("You are "..string.format ("%i", distance).." yards away from your target location.", "WHISPER", nil, name)
		end
		
		
		--reset the press counter
		DISP_PRESSCOUNTER = 0
	end
end


--set the value for TARGETEXISTS
--called from bwQuestCommands: bwQuestNext and bwQuestPrev
function hasQuest(funcParam)
	--1 for true there is target location, 0 for false there is not target location
	TARGETEXISTS = funcParam

end






