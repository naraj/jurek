﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SteeringCarFromAbove
{
    public enum MapPoint
    {
        FREE, OBSTACLE, CAR, PARKING
    }

    public class Map
    {
        public float distanceBetweenPoints = -666.666f; //in m
        public Point carPosition = new Point(-666, -666);
        public Point parkingPosition = new Point(-666, -666);
        public MapPoint[,] map;

        public int mapSizeX { get { return map.GetLength(0); } }
        public int mapSizeY { get { return map.GetLength(1); } }
    }

}
