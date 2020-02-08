
--Initialize bag functionality
function bwBagActivate()
   if(BWBAGMODE == false) then
	bwFlagSwitch("inventory")
   else
	BWBAGMODE = false
   end
   
   ITEMCOUNT = 0
   
   -- Get the indices of the items in bags
   local itemID
   local itemIndex = 0
   for bag = 0, 4 do
	BAGSIZE = GetContainerNumSlots(bag) -- Gets the size of the bag
	for ind = 1, BAGSIZE do
      itemID = GetContainerItemID(bag,ind)
      if (itemID ~= nil) then
         ITEMSLOTS[itemIndex] = {}
		 ITEMSLOTS[itemIndex][0] = bag
		 ITEMSLOTS[itemIndex][1] = ind
		 itemIndex = itemIndex + 1
      end
	end
   end
   
   NUMOFITEMS = table.getn(ITEMSLOTS) -- .getn() returns the size of the table
   
   BAG = ITEMSLOTS[0][0]      -- first BAG
   BAGINDEX = ITEMSLOTS[0][1] -- first BAGINDEX
   
   if(NUMOFITEMS == 1) then
      SendChatMessage("You have " .. (NUMOFITEMS + 1) .. " item in your bags.", "WHISPER", nil, name)
   else
      SendChatMessage("You have " .. (NUMOFITEMS + 1) .. " items in your bags.", "WHISPER", nil, name)
   end

end

--Gets the name of the current item.
function bwBagInfo()
   --Tooltip reader
   local f = CreateFrame('GameTooltip', 'tooltip', UIParent, 'GameTooltipTemplate')
   f:SetOwner(UIParent, 'ANCHOR_NONE')
   
   --Get the itemID of the item in the backpack
   local itemID = GetContainerItemID(BAG,BAGINDEX)
   
   if (itemID ~= nil) then
   --Set the tooltip to the item
   f:SetInventoryItemByID(itemID)
   
   --Get the text
   --Note, check for NumLines()
   ITEMNAME = tooltipTextLeft1:GetText()
   else
	ITEMNAME = "This item has been equipped or destroyed."
	end
   
   SendChatMessage(ITEMNAME, "WHISPER", nil, name)
end

--Gets the next item in the queue.
function bwBagNext()
   if (ITEMCOUNT + 1) > NUMOFITEMS then -- end of bag
	JocysCom_SendChatMessageStop(2)
      SendChatMessage("No more items.", "WHISPER", nil, name)
   else 
      ITEMCOUNT = ITEMCOUNT + 1
	  BAG = ITEMSLOTS[ITEMCOUNT][0]
      BAGINDEX = ITEMSLOTS[ITEMCOUNT][1]
      bwBagInfo()
   end
end

--Gets the previous item in the queue.
function bwBagPrev()
   if (ITEMCOUNT - 1) < 0 then -- first item in bag
   JocysCom_SendChatMessageStop(2)
      SendChatMessage("First item in the bag.", "WHISPER", nil, name)
   else
      ITEMCOUNT = ITEMCOUNT - 1
	  BAG = ITEMSLOTS[ITEMCOUNT][0]
      BAGINDEX = ITEMSLOTS[ITEMCOUNT][1]
      bwBagInfo()
   end
end

--Gets all information in the tooltip aside from the name.
--TODO: See about stats, get vendor value/money.
function bwBagMoreInfo()
   --Tooltip reader
   --NOTE: The second argument to the CreateFrame function creates a global variable
   --and may cause problems like empty lines if not unique and used for another item.
   local f = CreateFrame('GameTooltip', 'tooltip'..BAGINDEX, UIParent, 'GameTooltipTemplate')
   f:SetOwner(UIParent, 'ANCHOR_NONE')
   
   --Get the itemID of the item in the backpack
   --Note, find the max bag BAGSIZE (starts at 1)
   local itemID = GetContainerItemID(BAG,BAGINDEX)
   
   if (nil == itemID) then
	sendText = "This item has been equipped or destroyed."
	-- Send combined message.
	JocysCom_SendChatMessageStop(2)
	SendChatMessage(sendText, "WHISPER", nil, NAME)
	return
	end
   
   --Set the tooltip to the item
   f:SetInventoryItemByID(itemID)
   
   --Get left aligned text.
   local sendText = ""
   local getText
   for i = 2, _G["tooltip"..BAGINDEX]:NumLines() do
      local myText = _G["tooltip"..BAGINDEX.."TextLeft"..i]
      if (myText ~= nil) then
         getText = myText:GetText()
         if (nil ~= getText) then
            sendText = sendText.." "..getText.."." -- The period adds a pause for text-to-speech.
         end
      end
   end
   
   --Get right aligned text.
   for i = 2, _G["tooltip"..BAGINDEX]:NumLines() do
      local myText = _G["tooltip"..BAGINDEX.."TextRight"..i]
      if (myText ~= nil) then
         getText = myText:GetText()
         if (nil ~= getText) then
            sendText = sendText.." "..getText.."." -- The period adds a pause for text-to-speech.
         end
      end
   end

   -- Send combined message.
   JocysCom_SendChatMessageStop(2)
   SendChatMessage(sendText, "WHISPER", nil, NAME)
end

--Returns the item at the given BAGINDEX, ind.
--Updates globals BAG, BAGINDEX, ITEMCOUNT.
function bwBagItemAtIndex(ind)
   if (ind < 1 or ind > (NUMOFITEMS + 1)) then
   JocysCom_SendChatMessageStop(2)
      SendChatMessage("Item out of range.", "WHISPER", nil, NAME)
   else 
      ITEMCOUNT = (ind - 1)
	  BAG = ITEMSLOTS[(ind - 1)][0]
      BAGINDEX = ITEMSLOTS[(ind - 1)][1]
      bwBagInfo()
   end
end

-- Binds the item to the given slot.
function bwBagBindItem(slot)
   -- Attach item to cursor.
   PickupItem(GetContainerItemID(BAG,BAGINDEX))
   -- Bind it to the requested slot. Does nothing if out of bounds.
   PlaceAction(slot)
   ClearCursor()
   JocysCom_SendChatMessageStop(2)
   SendChatMessage("Item bound to "..slot..".", "WHISPER", nil, NAME)
end

-- There is an issue of updating after an item is equipped or destroyed.
-- The game seems to be working under the same environment as if the item
-- is in the bag. The work around was to restart the bag functionality
-- after calling equip or destroy, but the outcome is the same.
-- Even a step back (the calling function) is in the same environment. 
-- bagInfo and bagMoreInfo has been adjusted to handle absent items in the bag.
function bwBagEquip()
   if IsEquippedItem(ITEMNAME) then
   JocysCom_SendChatMessageStop(2)
      SendChatMessage("This item is already equipped.", "WHISPER", nil, NAME)
   else
      EquipItemByName(ITEMNAME)
   end
end

function bwBagDelete()
   PickupContainerItem(BAG,BAGINDEX)
   DeleteCursorItem()
   JocysCom_SendChatMessageStop(2)
   SendChatMessage("Deleted item.", "WHISPER", nil, NAME)
end
