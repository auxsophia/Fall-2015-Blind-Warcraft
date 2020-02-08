--Waypoint Utilities File.  Used for adding, removing, and parsing the waypoint db.

function bwuTEST(hello)

	print("hello, wow!")

end
--############
--[[In order to call functions for the waypoint object, you must use the ":"
	as in object:functionName. If you do not do this and instead use the ".",
	it changes the base object definition in bwuGlobals.lua instead of the
	current instance of the object.]]--
--############
--now we can use this new function to create individual waypoints
function bwWaypointAddWP()
	BWWPA[BWWAYINDEX] = waypoint:new()
	BWWPA[BWWAYINDEX].id = BWWAYINDEX
	BWWPA[BWWAYINDEX]:setCoords ()
	BWWPA[BWWAYINDEX]:setLinks ()
	BWWAYINDEX = BWWAYINDEX+1	
end

--this function deletes the last waypoint that was put down
function bwWaypointRemoveWP()
	BWWAYINDEX = BWWAYINDEX-1
	BWWPA[BWWAYINDEX] = nil
	collectgarbage(collect)
end

--prints out the waypoint at index input
function bwWaypointPrint(input)
	print("x = " .. BWWPA[input].x)
	print("y = " .. BWWPA[input].y)
	print("id = " .. BWWPA[input].id)
	linkvar = BWWPA[input].links
	for k, v in pairs(linkvar) do print(k, v) end
end

--attempts to print out the entire waypoint array
function printWParray()
	for i = 0, BWWAYINDEX-1 do
	bwWaypointPrint(i)
	end
end

--test function used for various tests
function wptest(input)

if(input == 1)then
	awp = waypoint:new()
	awp.id = 0
	awp.x = 10
	awp.y = 20
end
if(input == 2)then
	bwp = waypoint:new()
	bwp.id = 1
	bwp.x = 5
	bwp.y = 7
	bwp.links[0] = a
	bwp.linkCount = 1
end

end