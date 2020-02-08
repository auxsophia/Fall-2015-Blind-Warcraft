--Jehoshua Josue
--used for measuring the bearing and player location
--10/04/15 still testing version

distance = GetDistanceSqToQuest(2)
print("distance: ".. distance)

facing = GetPlayerFacing()
print("facing:"..facing)

local init_xPos, init_yPos = GetPlayerMapPosition("Mazgrind")
init_xPos = init_xPos*10
init_yPos = init_yPos*10

print("init_xPos: "..init_xPos)
print("init_yPos: "..init_yPos)