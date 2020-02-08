--Note: Globals should be CAPITALIZED by convention.
--

function bwFlagSwitch(argument)

bwQuestMode = false
BWSBMODE = false
BWBAGMODE = false
BWCHARMODE = false
--If there are any more modules we would need to activate and deactivate just place them here
if(argument == "") then
	print("no argument given")
elseif(argument == "spellbook") then
	BWSBMODE = true
elseif(argument == "inventory") then
	BWBAGMODE = true
elseif(argument == "quest") then
	bwQuestMode = true
elseif(argument == "character") then
	BWCHARMODE = true
end

print("FLAG STATUS")
print("-----------")
print("QuestMode " .. (bwQuestMode and 1 or 0))
print("SBMODE " .. (BWSBMODE and 1 or 0))
print("BAGMODE " .. (BWBAGMODE and 1 or 0))
print("BWCHARMODE " .. (BWCHARMODE and 1 or 0))
end

NAME = GetUnitName("player", 1)
playerUnitID = UnitGUID("player")
targetChanged = true
--selectedQuest = nil
ph = nil

--Quest Variables
selectedQuest = nil
bwQuestMode = false
bwRewardMode = false 
bwAcceptOrDeclineMode = false  
qIndex = 0
--QUESTREWARDTABLE = {}
REWARDSELECT = 1
QUESTCHANGE = false
--RLOADED = false 
--ITEMINDEX = 0

--SB Variables
CURRSBINDEX = nil
SBOFFSET = nil
MAXSBINDEX = nil
BWSBMODE = false
bwSBModePrevious = false
SPELLNAME, SPELLDESC, SPELLCASTTIME, SPELLMINRANGE, SPELLMAXRANGE, SPELLID, SPELLDESC = nil, nil, nil, nil, nil, nil, nil

--Bag Variables
BWBAGMODE = false
BAG = nil 		  -- The bag slot, 0-4
BAGINDEX = nil    -- Location of item in the bag
ITEMSLOTS = {}    -- Table of indices of items in the bag
ITEMCOUNT = nil   -- The current number of items iterated over
NUMOFITEMS = nil  -- The number of items in the bag
ITEMNAME = ""     -- Name of the current item

--Combat Variables
HEALTHCHECK75 = false
HEALTHCHECK50 = false
HEALTHCHECK25 = false
CURRHEALTHPERCENT = 1
INCOMBAT = false
TARGET = nil

--Character Info Variables
BWCHARMODE = false
EQUIPMENT = {}
BWCHARINDEX = 0
EQUIPMENT[19] = "Strength"
EQUIPMENT[20] = "Agility"
EQUIPMENT[21] = "Intellect"
EQUIPMENT[22] = "Stamina"
EQUIPMENT[23] = "CritStrike"
EQUIPMENT[24] = "Haste"
EQUIPMENT[25] = "Mastery"
EQUIPMENT[26] = "Spirit"
EQUIPMENT[27] = "BonusArmor"
EQUIPMENT[28] = "Multistrike"
EQUIPMENT[29] = "Leech"
EQUIPMENT[30] = "Versatility"
EQUIPMENT[31] = "Avoidance"
EQUIPMENT[32] = "AttackPower"
EQUIPMENT[33] = "SpellPower"
SLOTNAMES = {}
SLOTNAMES[0] = "Cloak"
SLOTNAMES[1] = "Chest"
SLOTNAMES[2] = "Feet"
SLOTNAMES[3] = "Ring 1"
SLOTNAMES[4] = "Ring 2"
SLOTNAMES[5] = "Gloves"
SLOTNAMES[6] = "Head"
SLOTNAMES[7] = "Legs"
SLOTNAMES[8] = "Main Hand"
SLOTNAMES[9] = "Necklace"
SLOTNAMES[11] = "Off Hand"
SLOTNAMES[12] = "Shirt"
SLOTNAMES[13] = "Shoulders"
SLOTNAMES[14] = "Tabard"
SLOTNAMES[15] = "Trinket 1"
SLOTNAMES[16] = "Trinket 2"
SLOTNAMES[17] = "Belt"
SLOTNAMES[18] = "Wrists"