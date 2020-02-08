

Object = {member1 = 10, member2 = 20, table1 = {}}

function Object:new (o)
	o = o or {}
	self.__index = self
	setmetatable(o, self)
	return o
end

function Object:printMembers ()

	print("member1 = " .. self.member1)
	print("member2 = " .. self.member2)
	print("table1 = ")
	t = self.table1
	for k, v in pairs(t) do print(k,v) end
end

function Object:setMembers (a,b,c,d)

	self.member1 = a
	self.member2 = b
	t = self.table1
	t[0] = c
	t[1] = d

end

function instantiateObject()
	newObject = Object:new ()
	
	newObject:printMembers ()
	
	Object:setMembers (5,6,7,8)
	newObject:printMembers ()

end
--[[copy/pasted shit ]]--
    Account = {balance = 0, member6}
    
    function Account:new (o)
      o = o or {}
      setmetatable(o, self)
      self.__index = self
      return o
    end
    
    function Account:deposit (v)
      self.balance = self.balance + v
	  self.member6 = 34
    end
    
    function Account:withdraw (v)
      if v > self.balance then error"insufficient funds" end
      self.balance = self.balance - v
    end
	
function AccountStuff()
	a = Account:new()
	b = Account:new()
	print(a.balance)
	print(b.balance)
	a:deposit(100)
	print(a.balance)
	print(b.balance)
end