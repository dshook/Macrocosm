using System;
using UnityEngine;

//Circular buffer of points to store where snek has been
public class PositionList{

  Vector2[] positions = new Vector2[32];

  int headIndex = 0;

  public int Count {
    get{ return positions.Length; }
  }

  public void Add(Vector2 position){
    positions[headIndex] = position;

    headIndex = (headIndex + 1) % positions.Length;
  }

  //head index offset should be negative
  public Vector2 Get(int headIndexOffset){
    var currentIndex = headIndex - 1 + headIndexOffset;
    if(currentIndex > positions.Length - 1){
      currentIndex = currentIndex % positions.Length;
    }else if (currentIndex < 0){
      currentIndex += positions.Length;
    }

    return positions[currentIndex];
  }

  public void SetSnakeSize(int snakeLength, int lengthMult){
    var neededSize = (snakeLength + 1) * lengthMult;
    if(positions.Length < neededSize){
      Array.Resize(ref positions, neededSize);
    }
  }

  public Vector2 GetNextInLine(float idealDistance){
    float distAccum = 0;
    var prevPoint = Get(0);

    for(var i = 1; i < positions.Length; i++){
      var pointAtOffset = Get(-i);
      var pointDistance = Vector2.Distance(prevPoint, pointAtOffset);
      distAccum += pointDistance;

      if(distAccum > idealDistance){
        //lerping between points to get the just right distance hopefully
        var lerp = distAccum - idealDistance;
        return Vector2.Lerp(prevPoint, pointAtOffset, lerp);
        // return prevPoint;
      }else{
        prevPoint = pointAtOffset;
      }
    }

    return prevPoint;
  }
}