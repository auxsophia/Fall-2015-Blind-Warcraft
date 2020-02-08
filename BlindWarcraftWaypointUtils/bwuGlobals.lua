--Waypoint Variables
waypoint = {x = 0, y = 0, id = 0, zone = nil, links = nil, linkCount = 0, quests = nil}

function waypoint:new (o)
  o = o or {}
  self.__index = self
  setmetatable(o, self)
  return o
end
--############
--[[If you want to create new waypoint functions (like class functions)
	then you must define them as such
		function waypoint:functionName (args)
		 //stuff happens
		end
	otherwise they will not be defined
	if you want to access and members or other functions while in the waypoint,
	you must reference them using the "." for variables and ":" for functions
	in this implementation, "self" is equivalent to "this"]]--
--############

function waypoint:setCoords ()
	xCoord, yCoord = GetPlayerMapPosition(NAME)
	xCoord = xCoord*100
	yCoord = yCoord*100
--[[	if(xCoord ~= nil) then
	print("xCoord is not nil")
	end
	if(yCoord ~=nil) then
	print("yCoord is not nil")
	end
	if(xCoord == nil) then
	print("xCoord is nil")
	end
	if(yCoord == nil) then
	print("yCoord is nil")
	end]]--

	self.x = xCoord
	self.y = yCoord
end

function waypoint:getCoords ()
	return self.x, self.y
end

function waypoint:setZone (input)
	self.zone = input
end

function waypoint:setLinks ()
	self.links = {}
	if(BWWAYINDEX ~= 0) then
		self.links[0] = BWWAYINDEX-1
		self.linkCount = self.linkCount+1
		end
end

function waypoint:setQuests (input)
	--nothing here
end

function waypoint:raiseLinkCount ()
	self.linkCount = self.linkCount+1
end

function waypoint:printWP()

	print("x = " .. self.x)
	print("y = " .. self.y)
	print("id = " .. self.id)
	wp = self.links
	count = 0
	print("has " .. self.linkCount .. " links:")

	for k, v in pairs(wp) do print(k, v) end

end


ADJMATRIX = {}
BWWAYPOINT = {}
BWWAYINDEX = 0

--BWWPA is the waypoint array
BWWPA = {}
