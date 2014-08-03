﻿using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteeringCarFromAbove;

namespace SteeringCarFromAboveTests
{
    [TestClass]
    public class TrackPlannerTests
    {
        [TestMethod]
        public void TrackPlannerFindSmallForwardTrackTest()
        {
            TrackPlanner planner = new TrackPlanner(
                locationTolerance: 9.0d, angleTolerance: 19.0d,
                positionStep: 10.0d, angleStep: 20.0d);

            Map map = new Map(2, 2, 2);

            map.car = new PositionAndOrientation(_x: 0.0d, _y: 0.0d, _angle: 90.0d);
            map.parking = new PositionAndOrientation(_x: 0.0d, _y: 10.0d, _angle: 90.0d);
            
            List<PositionAndOrientation> track = planner.PlanTrack(map);
            Assert.AreEqual(1, track.Count);

        }

        [TestMethod]
        public void TrackPlannerFindLongForwardTrackTest()
        {
            TrackPlanner planner = new TrackPlanner(9.0d, 19.0d, 10.0d, 20.0d);

            Map map = new Map(2, 2, 2);

            map.car = new PositionAndOrientation(_x: 0.0d, _y: 0.0d, _angle: 90.0d);
            map.parking = new PositionAndOrientation(_x: 0.0d, _y: 100.0d, _angle: 90.0d);

            List<PositionAndOrientation> track = planner.PlanTrack(map);
            Assert.AreEqual(10, track.Count);
        }

        [TestMethod]
        public void TrackPlannerFind90TurnTrackTest()
        {
            TrackPlanner planner = new TrackPlanner(9.0d, 19.0d, 10.0d, 20.0d);

            Map map = new Map(2, 2, 2);

            map.car = new PositionAndOrientation(_x: 0.0d, _y: 0.0d, _angle: 90.0d);
            map.parking = new PositionAndOrientation(_x: 100.0d, _y: 100.0d, _angle: 0.0d);

            List<PositionAndOrientation> track = planner.PlanTrack(map);
            Assert.IsTrue(track.Count > 10);  // coz it not a bad expectation ;)
        }
        
        //CODE IS TOO SLOW FOR THAT
        //[TestMethod]
        //public void TrackPlannerFind360TurnTrackTest()
        //{
        //    TrackPlanner planner = new TrackPlanner(9.0d, 19.0d, 10.0d, 20.0d);

        //    Map map = new Map(2, 2, 2);

        //    map.car = new PositionAndOrientation(_x: 0.0d, _y: 0.0d, _angle: 90.0d);
        //    map.parking = new PositionAndOrientation(_x: 0.0d, _y: 0.0d, _angle: 90.0d);

        //    List<PositionAndOrientation> track = planner.PlanTrack(map);
        //}
    }
}
