﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace SteeringCarFromAbove
{
    //TODO: Track can be found with divide and conquer method to decrease complexity
    // at first find track with big step and tolerance, then find half track with smaller step and tolerance

    //TODO: CHANGE ALL CODE TO RADIANS!!!
    //TODO: add obstacles
    
    //NOTE: target can be not found becase of being close to target before but not exactly there what would block the track
    //TODO: increase target finding tolerance if that problem occurs
    public class TrackPlanner
    {
        //public for drawing for now
        public class BFSNode : IQuadStorable
        {
            public BFSNode(PositionAndOrientation _position, BFSNode _predecessor)
            {
                position = _position;
                predecessor = _predecessor;
                Rect = new Rectangle((int)position.x, (int)position.y, 1, 1);
            }

            public PositionAndOrientation position;
            public BFSNode predecessor;

            public Rectangle Rect { get; private set; }
            public bool HasMoved { get { return false; } }
        }

        private class Obstacle : IQuadStorable
        {
            public Obstacle(Rectangle _rect)
            {
                Rect = _rect;
            }

            public Rectangle Rect { get; private set; }
            public bool HasMoved { get { return false; } }
        }

        public List<BFSNode> GetSeen()
        {
            return seen_.GetAllObjects();
        }

        public TrackPlanner(int locationTolerance, double angleTolerance, double positionStep, double angleStep,
            double mapSizeX, double mapSizeY)
        {
            locationToleranceSquared_ = locationTolerance * locationTolerance;
            locationTolerance_ = locationTolerance;
            angleTolerance_ = angleTolerance;
            positionStep_ = positionStep;
            angleStep_ = angleStep;
            mapSizeX_ = mapSizeX;
            mapSizeY_ = mapSizeY;

            seen_ = new QuadTree<BFSNode>(0, 0, (int)mapSizeX, (int)mapSizeY);
        }

        //TODO: add version with recalc from 80% with better accuaracy
        /// <summary>
        /// it finishes when gets to target
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public List<PositionAndOrientation> PlanTrack(Map map)
        {
            if (seen_.GetAllObjects().Count != 0)
                throw new ApplicationException();

            obstacles_ = map.obstacles;

            Queue<BFSNode> frontier = new Queue<BFSNode>();
            BFSNode startPoint = new BFSNode(map.car, null);
            frontier.Enqueue(startPoint);

            PositionAndOrientation target = map.parking;

            while (frontier.Count != 0)
            {
                BFSNode curr = frontier.Dequeue();

                List<BFSNode> succesors = generateSuccessors(curr);

                foreach (BFSNode s in succesors)
                {
                    if (ArePointsSame(s.position, target))
                        return GetPathBetweenPoints(startPoint, s);
                }

                succesors.ForEach(x => frontier.Enqueue(x));
                succesors.ForEach(x => seen_.Add(x));
            }

            return new List<PositionAndOrientation>();  // couldn't find result - returning empty list
        }

        private Map preparedMap_ = null;
        private BFSNode startPoint_ = null;
        public void PrepareTracks(Map map)
        {
            seen_.Clear();

            preparedMap_ = map; // hack a bit - we r holding state here

            obstacles_ = map.obstacles;

            Queue<BFSNode> frontier = new Queue<BFSNode>();
            startPoint_ = new BFSNode(map.car, null);
            frontier.Enqueue(startPoint_);

            PositionAndOrientation target = map.parking;

            while (frontier.Count != 0)
            {
                BFSNode curr = frontier.Dequeue();

                List<BFSNode> succesors = generateSuccessors(curr);

                succesors.ForEach(x => frontier.Enqueue(x));
                succesors.ForEach(x => seen_.Add(x));
            }
        }

        public List<PositionAndOrientation> GetTrackFromPreparedPlanner(PositionAndOrientation target)
        {
            if (preparedMap_ == null)
                throw new ApplicationException("Prepare tracks first!");

            if (seen_.Count == 0)
                throw new ApplicationException();

            double smallestError = Double.MaxValue;
            BFSNode nodeClosestToTarget = null;
            foreach (BFSNode x in seen_)
            {
                double currError = Math.Pow(x.position.x - target.x, 2.0d) +
                    Math.Pow(x.position.y - target.y, 2.0d) +
                    Math.Pow(x.position.angle - target.angle, 2.0d);

                if (currError < smallestError)
                {
                    smallestError = currError;
                    nodeClosestToTarget = x;
                }
            }

            return GetPathBetweenPoints(startPoint_, nodeClosestToTarget);
        }

        private List<PositionAndOrientation> GetPathBetweenPoints(BFSNode start, BFSNode target)
        {
            List<PositionAndOrientation> path = new List<PositionAndOrientation>();

            BFSNode curr = target;
            while (curr != null)
            {
                path.Add(curr.position);
                curr = curr.predecessor;
            }

            path.Reverse();

            return path;
        }

        private List<BFSNode> generateSuccessors(BFSNode predecessor)
        {
            List<BFSNode> successors = new List<BFSNode>();

            double oldAngleInRadians = predecessor.position.angle / 180.0f * Math.PI;
            double angleStepInRadians = angleStep_ / 180.0f * Math.PI;
            foreach (int i in new List<int>(){0, -1, 1})
            {
                double newAngleInRadians = oldAngleInRadians + angleStepInRadians * i;
                double newX = predecessor.position.x + Math.Cos(newAngleInRadians) * positionStep_;
                double newY = predecessor.position.y + Math.Sin(newAngleInRadians) * positionStep_;

                if (newX < 0 || newX > mapSizeX_ || newY < 0 || newY > mapSizeY_)
                    continue;

                double newAngle = predecessor.position.angle + angleStep_ * i;
                PositionAndOrientation newPosition = new PositionAndOrientation(newX, newY, newAngle);

                if (!IsPositionSeen(newPosition) && !(IsPositionObstacled(newPosition)))
                {
                    BFSNode newNode = new BFSNode(newPosition, predecessor);
                    successors.Add(new BFSNode(newPosition, predecessor));
                }
            }

            return successors;
        }

        private bool IsPositionSeen(PositionAndOrientation point)
        {
            List<BFSNode> matchingPositionNodes = seen_.GetObjects(new Rectangle(
                (int)point.x - locationTolerance_ / 2, (int)point.y - locationTolerance_ / 2,
                locationTolerance_, locationTolerance_));

            return matchingPositionNodes.Any(
                x => MathTools.AnglesEqual(x.position.angle, point.angle, angleTolerance_));
        }

        private bool IsPositionObstacled(PositionAndOrientation point)
        {
            Rectangle pointRectangle = new Rectangle((int)point.x, (int)point.y, 1, 1);

            return obstacles_.Any(x => x.Contains(pointRectangle));
        }

        private bool ArePointsSame(PositionAndOrientation a, PositionAndOrientation b)
        {
            return Math.Pow(a.x - b.x, 2.0d) + Math.Pow(a.y - b.y, 2.0d) <= locationToleranceSquared_
                &&
                 MathTools.AnglesEqual(a.angle, b.angle, angleTolerance_);
        }

        private double locationToleranceSquared_; // squared here for optimization
        private int locationTolerance_;
        private double angleTolerance_;
        private double positionStep_;
        private double angleStep_;
        private double mapSizeX_;
        private double mapSizeY_;
        private QuadTree<BFSNode> seen_;
        private List<Rectangle> obstacles_;
    }
}
