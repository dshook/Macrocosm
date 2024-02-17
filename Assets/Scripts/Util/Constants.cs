using System;
using UnityEngine;

public static class Constants {
  public const UInt64 SPEED_OF_LIGHT = 299792458;
  public const UInt64 LIGHT_YEAR = 9460730472580800; //ly -> m
  public const UInt64 AU_M = 149597900000; //Astronomical Unit in M
  public const double G = 9.80665; // Earth gravity in m/s^2
  public const float EARTH_DIAMETER_M = 12742000f; // Earth diameter in meters
  public const UInt64 SECONDS_PER_YEAR = 31556952;

  public const float MARS_EQUIV_DIAMETER_M = 6093676;

  public static Vector3 defaultCameraPosition = new Vector3(0, 0, -10);
  public static float defaultCameraOrthoSize = 5;

  //Calculated to be 1 frame at 15 fps
  public static float oneFrameTimeWorstCase = 0.03f;

  //Calculated to try to maintain 50 fps
  public static float spawnTimeFrameBudget = 0.02f;

  public const int MAX_DEMO_STAGE = 4;

#if UNITY_EDITOR
  public static string reportUrl = "http://localhost:9010";
#else
  public static string reportUrl = "report-server";
#endif

  public static string reportProjectId = "";
}