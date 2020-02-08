PATHWAY = {} --will hold the waypoints that the player will need to follow to get to the target location
PATHWAY_SIZE = 0
x1 = 0
y1 = 0
x2 = 0
y2 = 0
function heuristic()
	distX = (x2 - x1)^2
	distY = (y2 - y1)^2
	return math.sqrt(distX + distY)

end

function findPath()
	--reset pathway array and its size
	PATHWAY = {}
	PATHWAY_SIZE = 0

	--Testing purposes
	endX = BWWPA2[5].x
	endY = BWWPA2[5].y
	--pretend that we determined the end location to be closest to node 5
	endNodeID = 5
	--pretend we determined the closest node to start with
	startNodeID = 0

	neighborCount = 0	--used to traverse array of links of each waypoint object


	i = startNodeID
	currentWP = BWWPA2[i]
	--outer loop is going through array of waypoints
	while currentWP.id ~= endNodeID do
		currentWP = BWWPA2[i]	--update current node
		curNeighborCount = currentWP.linkCount

		x1 = currentWP.x
		y1 = currentWP.y
		x2 = endX
		y2 = endY
		curCost = heuristic()

		--base case is the shortest path to the target IS the current node
		bestMoveCost = curCost
		bestMoveIndex = currentWP.id
		--this inner loop is going through the array of links inside each waypoint object
		for x=0,curNeighborCount-1 do
			--newCost = costSoFar[i] + graph.cost??
			nextWP = BWWPA2[currentWP.links[x]]

			--determine which next node would be closer to the end location
			x1 = nextWP.x
			y1 = nextWP.y
			x2 = endX
			y2 = endY
			newCost = heuristic(nextWP.x, nextWP.Y, endX, endY)
			if newCost <= bestMoveCost  then
				bestMoveCost = newCost
				bestMoveIndex = nextWP.id
			end
			PATHWAY[PATHWAY_SIZE] = bestMoveIndex
			i = bestMoveIndex

			PATHWAY_SIZE = PATHWAY_SIZE + 1
		end

		d = currentWP.id	--DEBUG
		print("curNode: "..d.."added: "..bestMoveIndex) --DEBUG

		if BWWPA2[i].id == endNodeID then
			break;
		end

	end  --end of while loop
end
