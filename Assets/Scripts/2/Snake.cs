using System.Collections.Generic;
using MoreMountains.NiceVibrations;
using strange.extensions.mediation.impl;
using UnityEngine;
using Shapes;


public class Snake : View {

  [Inject] InputService input {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageTwoDataModel stageTwoData { get; set; }
  [Inject] ObjectPool objectPool { get; set; }
  [Inject] AudioService audioService {get; set;}
  [Inject] CameraService cameraService {get; set;}

  public bool playerControlled = false;
  public bool aggressiveAI = false;
  public float minDistance = 0.3f;
  public float rotationSpeed = 4.5f;
  public float maxTurnAngle = 15f;
  public float speed = 1.6f;
  public int positionIndexMult = 4;

  public AnimationCurve turnDampeningCurve;

  bool isBoosting = false;
  float boostTime = 0f;
  float boostSpeed = 0f;
  public float BoostTime {get {return boostTime; }}

  public GameObject startingObject;
  public GameObject headPointer;
  public GameObject boostTrailPrefab;
  public GameObject snakeLinkPrefab;
  public JoystickControl playerJoystickControl;

  public AudioClip breakSound;
  public AudioClip[] eatSound;
  int eatSoundIdx = 0;

  Vector2 moveToPos;
  GameObject atomHolder;
  GameObject snakeHolder;
  ParticleSystem boostTrail;

  SnakeHead head;

  float immuneTimer = 0f;
  const float immuneTime = 0.25f;
  bool isImmune = false;

  public int eatSequenceIndex = 0;

  public class SnakeMember{
    public GameObject gameObject;
    public Transform transform;
    public SnakeSegment snakeSegment;
    public int size;
    public bool isHead;
    public SnakeLink link;
  }
  public List<SnakeMember> snakeMembers = new List<SnakeMember>();
  PositionList positions = new PositionList();

  //Stuff From StageTwoManager
  public HashSet<AtomRenderer> freeAtoms;
  public ObjectRepositioner objectRepositioner;

  protected override void Awake () {
    base.Awake();

    atomHolder = GameObject.Find("Stages/2/Particles");
    snakeHolder = GameObject.Find("Stages/2/Snakes");

    objectPool.CreatePool(snakeLinkPrefab, 0);
  }

  bool isSetup = false;

  public void SetupSnake(bool isInitialCall){
    if(atomHolder == null){
      Awake();
    }
    if(stageRules == null){
      base.Start();
    }

    eatSequenceIndex = 0;
    eatSoundIdx = 0;
    moveToPos = new Vector2(10000, 0);

    int startingObjectCount = 0;
    bool usingSavedAtoms = playerControlled && isInitialCall && stageTwoData.snakeAtoms.Count > 0;

    if(usingSavedAtoms){
      startingObjectCount = stageTwoData.snakeAtoms.Count;
    }else{
      startingObjectCount = stageRules.StageTwoRules.snakeStartLength != 0
        ? stageRules.StageTwoRules.snakeStartLength
        : stageRules.StageTwoRules.eatSequence.Length;
    }

    positions.SetSnakeSize(startingObjectCount, positionIndexMult);

    for (int p = 0; p < startingObjectCount; p++) {

      var position = new Vector3(0, -p * minDistance, 0) + transform.position;
      var size = stageRules.StageTwoRules.eatSequence[eatSequenceIndex];
      //And the indicated next atom
      if(usingSavedAtoms){
        //Skip using the saved size for the head so we don't ever get "desynced" between what's saved
        if(p == 0){
          size = stageRules.StageTwoRules.eatSequence[stageTwoData.eatSequenceIndex];
        }else{
          size = stageTwoData.snakeAtoms[p];
        }
      }
      var snakeMember = CreateSnakeMember(size, p, position);

      if(p == 0){
        head = snakeMember.gameObject.AddComponent<SnakeHead>();
        head.snake = this;
        snakeMember.isHead = true;

        //set the head color based on the next index we need
        head.color = HeadColor(size);

        var body = snakeMember.gameObject.GetComponent<SnakeSegment>();
        Destroy(body);

        //make a direction pointer
        var newHeadPointer = Instantiate(headPointer, Vector3.zero, Quaternion.identity);
        newHeadPointer.transform.SetParent(head.transform, false);

        //make the boost trail
        var newBoostTrail = Instantiate(boostTrailPrefab, Vector3.zero, Quaternion.identity);
        newBoostTrail.transform.SetParent(head.transform, false);
        boostTrail = newBoostTrail.GetComponent<ParticleSystem>();

        //make sure we're pointing in the right direction for initial movement
        head.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector2.up);
      }else{
        var snakeSegment = snakeMember.gameObject.GetComponent<SnakeSegment>();
        snakeSegment.snake = this;
        snakeMember.snakeSegment = snakeSegment;
        snakeMember.snakeSegment.onEaten();

        //Create the snake link and pointers
        var newLink = objectPool.Spawn(snakeLinkPrefab, transform, Vector3.zero);
        var snakeLink = newLink.GetComponent<SnakeLink>();
        var previousMember = snakeMembers[p - 1];
        snakeLink.towardsHead = previousMember.transform;
        snakeLink.towardsTail = snakeMember.transform;

        snakeMember.link = snakeLink;

      }

      //bump up the eat sequence while setting up the inital snake
      eatSequenceIndex = (eatSequenceIndex + 1) % stageRules.StageTwoRules.eatSequence.Length;
    }

    //seed starting positions, have to do reverse order to add the points in the right order,
    //also skip iterating on the head
    for(var s = startingObjectCount - 1; s > 0; s--){
      var pointsBetweenPositions = 4;
      var curPoint = snakeMembers[s].transform.position;
      var nextPoint = snakeMembers[s - 1].transform.position;
      for(var i = 0; i < pointsBetweenPositions; i++){
        positions.Add(Vector2.Lerp(curPoint, nextPoint, (float)i / pointsBetweenPositions));
      }
    }


    eatSequenceIndex = 0;
    isSetup = true;
  }

  SnakeMember CreateSnakeMember(int size, int index, Vector3 position){
    var item = Instantiate(startingObject, Vector3.zero, Quaternion.identity, transform);
    FloaterToSnakePart(item, this, transform);

    var atomRenderer = item.GetComponentInChildren<AtomRenderer>();

    atomRenderer.size = size;
    item.transform.position = position;
    item.transform.parent = transform;

    var snakeMember = new SnakeMember{
      gameObject = item,
      transform = item.transform,
      size = atomRenderer.size,
      isHead = false
    };

    snakeMembers.Add(snakeMember);

    return snakeMember;
  }

  public void Cleanup(){
    snakeMembers.Clear();
    transform.DestroyChildren();
    eatSequenceIndex = 0;

    isSetup = false;
  }

  void Update () {
    //wait till snake is setup
    if(!isSetup){ return; }

    var headTransform = snakeMembers[0].transform;

    if(playerControlled){
      moveToPos = (Vector2)headTransform.position + (playerJoystickControl.direction * 5f);
      // #if UNITY_EDITOR
      // if(input.ButtonIsDown() && !playerJoystickControl.Dragging){
      //   moveToPos = input.pointerWorldPosition;
      // }
      // #endif
    }else{
      //Reset the eat sequence index when resetting the stage progress so we don't array bounds error
      if(eatSequenceIndex > stageRules.StageTwoRules.eatSequence.Length - 1){ eatSequenceIndex = 0; }

      //if an enemy snake is reduced to just its head, and close to the player, move them far away
      if(snakeMembers.Count == 1 && Vector2.Distance(headTransform.position, cameraService.Cam.transform.position) < objectRepositioner.innerRadius){
        headTransform.position = RandomExtensions.RandomPointInCircle(objectRepositioner.innerRadius, objectRepositioner.outerRadius, headTransform.position);
      }

      //find the nearest atom we need and go for that
      var needSize = stageRules.StageTwoRules.eatSequence[eatSequenceIndex];
      var minFoundDist = float.MaxValue;

      foreach(var freeAtom in freeAtoms){

        //skip all the things we don't want
        if(freeAtom.size != needSize){ continue; }

        var dist = Vector2.Distance(headTransform.position, freeAtom.transform.position);
        if(dist < minFoundDist){
          minFoundDist = dist;
          moveToPos = freeAtom.transform.position;
        }
      }

      if(aggressiveAI){
        //seek and destroy the player snake
        for(int i = 0; i < snakeHolder.transform.childCount; i++){
          var child = snakeHolder.transform.GetChild(i);
          var otherSnake = child.GetComponent<Snake>();
          if(otherSnake == null || otherSnake == this || !otherSnake.playerControlled) continue;

          var otherSnakeMembers = otherSnake.snakeMembers;
          for(int s = 1; s < otherSnakeMembers.Count; s++){
            var segment = otherSnakeMembers[s];

            var dist = Vector2.Distance(headTransform.position, segment.transform.position);
            if(dist < minFoundDist){
              minFoundDist = dist;
              moveToPos = segment.transform.position;
            }
          }
        }
      }
    }

    //update boost
    if(isBoosting){
      //Deplete boost time
      boostTime = Mathf.Max(0, boostTime -= Time.deltaTime);

      if(boostTime > 0){
        boostSpeed = stageRules.StageTwoRules.boostSpeed;
      }else{
        //decay boostspeed
        boostSpeed = Mathf.Max(0, boostSpeed - (Time.deltaTime * 1.2f));
      }
    }else{
      //Recharge boost time
      boostTime = Mathf.Min(stageRules.StageTwoRules.boostAmount, boostTime += Time.deltaTime);

      //decay boostspeed
      boostSpeed = Mathf.Max(0, boostSpeed - (Time.deltaTime * 1.2f));
    }

    //control the boost trail
    #pragma warning disable
    boostTrail.enableEmission = boostSpeed > 0;
    #pragma warning restore

    //if we're close to the click point reset it to be far away in the direction we're pointing so we don't stop or spin in circles
    if(Vector2.Distance(moveToPos, headTransform.position) < 0.4f){
      moveToPos = (Vector2)headTransform.position + (Vector2)(headTransform.rotation * Vector2.up) * 100f;
    }
    Move();

    if(isImmune){
      immuneTimer += Time.deltaTime;

      if(immuneTimer >= immuneTime){
        isImmune = false;
        immuneTimer = 0f;
      }
    }


    //have to manually check for head colliding into its own body
    var headMember = snakeMembers[0];
    var headCollider = headMember.gameObject.GetComponentInChildren<CircleCollider2D>();
    var headColliderSize = headCollider.radius * headCollider.transform.localScale.x;
    var startCheckingIndex = 2; //don't check right near the head so we can maintain an actual snake

    for(int s = startCheckingIndex; s < snakeMembers.Count; s++){
      var segment = snakeMembers[s];
      var dist = Vector2.Distance(headMember.transform.position, segment.transform.position);
      if(dist < 2f * headColliderSize){
        Break(segment.gameObject, true);
        break;
      }
    }

    //as well as other snakes
    for(int i = 0; i < snakeHolder.transform.childCount; i++){
      var child = snakeHolder.transform.GetChild(i);
      var otherSnake = child.GetComponent<Snake>();
      if(otherSnake == null || otherSnake == this) continue;

      var otherSnakeMembers = otherSnake.snakeMembers;
      for(int s = 1; s < otherSnakeMembers.Count; s++){
        var segment = otherSnakeMembers[s];
        var dist = Vector2.Distance(headMember.transform.position, segment.transform.position);
        if(dist < 2f * headColliderSize){
          //break up the other snake and eat the piece
          // var atomRenderer = segment.gameObject.GetComponent<AtomRenderer>();
          if(otherSnake.Break(segment.gameObject, false)){
            //Same as eating your own tail, move the hit one away so you can't pump your score going in circles
            // segment.gameObject.transform.position = segment.gameObject.transform.position + (Vector3.right * 400f);
            Eat(segment.gameObject.GetComponentInChildren<AtomRenderer>(), null);
          }
          break;
        }
      }
    }
  }

  public void Eat(AtomRenderer nom, Collision2D col)
  {
    //wait till snake is setup
    if(!isSetup){ return; }

    //Check to see if they hit the right kind of atom in the sequence
    if(nom.size != stageRules.StageTwoRules.eatSequence[eatSequenceIndex]){
      //If not and it's a regular collision, make the other atom bounce off more
      if(col != null){
        var direction = (col.rigidbody.transform.position - col.otherRigidbody.transform.position).normalized;
        col.rigidbody.AddForce(direction * 5f);
      }
      return;
    }

    var nomGO = nom.transform.parent.gameObject;

    FloaterToSnakePart(nomGO, this, transform);

    var snakeMember = new SnakeMember{
      gameObject = nomGO,
      transform = nomGO.transform,
      snakeSegment = nomGO.GetComponent<SnakeSegment>(),
      size = nom.size,
    };

    //Put the new guy at the end of the line, used to put them at the beginning but that now causes the whole
    //snake to stop or move backwards in a weird looking way
    snakeMember.transform.position = snakeMembers[snakeMembers.Count - 1].transform.position;

    snakeMembers.Add(snakeMember);

    positions.SetSnakeSize(snakeMembers.Count, positionIndexMult);

    //update our head and the sequence
    eatSequenceIndex = (eatSequenceIndex + 1) % stageRules.StageTwoRules.eatSequence.Length;
    head.color = HeadColor(stageRules.StageTwoRules.eatSequence[eatSequenceIndex]);
    snakeMembers[0].size = head.atomRenderer.size = stageRules.StageTwoRules.eatSequence[eatSequenceIndex];

    //Create the snake link and pointers
    var newLink = objectPool.Spawn(snakeLinkPrefab, transform, Vector3.zero);
    var snakeLink = newLink.GetComponent<SnakeLink>();
    snakeLink.towardsHead = snakeMembers[snakeMembers.Count - 2].transform;
    snakeLink.towardsTail = snakeMember.transform;
    snakeMember.link = snakeLink;

    isImmune = true;

    snakeMember.snakeSegment.onEaten();

    if(playerControlled){
      MMVibrationManager.Haptic(HapticTypes.MediumImpact);

      audioService.PlaySfx(eatSound[eatSoundIdx]);
      eatSoundIdx = (eatSoundIdx + 1) % eatSound.Length;
    }

    var punchTweenDuration = 0.5f;
    for(int i = 0; i < snakeMembers.Count; i++){
      var punchScale = i == 0 ? Vector3.one * 1.75f : Vector3.one * 1.55f;
      LeanTween.cancel(snakeMembers[i].gameObject);
      snakeMembers[i].transform.localScale = Vector3.one;
      LeanTween.scale(snakeMembers[i].gameObject, punchScale, punchTweenDuration).setDelay(i * 0.07f).setEase(LeanTweenType.punch);
    }
  }

  public bool Break(GameObject segmentHit, bool isHeadHit){
    //Immunity prevents the broken off pieces immediately cascading up the snake breaking off more than it should
    if(isImmune) return false;

    if(snakeMembers == null){
      Debug.Log("snakeMembers are null");
      return false;
    }

    //Find where segment hit is in the chain
    var hitIndex = snakeMembers.FindIndex(member => member.gameObject == segmentHit);
    if(hitIndex == -1){
      Debug.Log("Couldn't find hit index");
      return false;
    }

    //Give some immune time to snake members when they've been eaten for head collisions
    if(isHeadHit && snakeMembers[hitIndex].snakeSegment.isImmune){
      return false;
    }

    for(int i = snakeMembers.Count - 1; i >= hitIndex; i--){
      var castaway = snakeMembers[i];
      SnakePartToFloater(castaway.gameObject, atomHolder.transform);

      //Kill the link
      if(castaway.link != null){
        castaway.link.towardsHead = castaway.link.towardsTail = null;
        objectPool.Recycle(castaway.link.gameObject);
      }

      castaway.snakeSegment.onBreak();
      snakeMembers.RemoveAt(i);

      //For head hits teleport the hit piece far away so you can't go around in circles eating your
      //own tail and pumping up your score
      if(isHeadHit && i == hitIndex){
        castaway.gameObject.transform.position = castaway.gameObject.transform.position + (Vector3.right * 400f);
      }
    }
    positions.SetSnakeSize(snakeMembers.Count, positionIndexMult);

    if(playerControlled){
      MMVibrationManager.Haptic(HapticTypes.SoftImpact);

      audioService.PlaySfx(breakSound);
      eatSoundIdx = 0;
    }

    isImmune = true;
    return true;
  }

  public void OnDrawGizmos(){
    var color = Color.magenta.SetA(0.3f);
    for(var i = 0; i < positions.Count; i++){
      Draw.Disc(positions.Get(i), 0.02f, color);
    }
  }

  public void Move()
  {
    var head = snakeMembers[0];
    var headTransform = head.transform;
    var moveSpeed = speed + boostSpeed;

    headTransform.Translate(headTransform.up * moveSpeed * Time.smoothDeltaTime, Space.World);

    Vector3 vectorToTarget = (Vector3)moveToPos - headTransform.position;

    var rotationAdjustment = 1f;

    if(snakeMembers.Count >= 3){
      var second = snakeMembers[1].transform.position;
      var third = snakeMembers[2].transform.position;
      var bendAngle = Vector2.SignedAngle(headTransform.position - second, third - second);
      bendAngle += bendAngle > 0 ? -180f : 180f;

      var turnDampeningAngleOffset = maxTurnAngle / 2f;

      //Dampen down how fast you can turn as you approach the max turn angle
      rotationAdjustment = turnDampeningCurve.Evaluate( Mathf.Clamp01(Mathf.Abs(bendAngle) / maxTurnAngle) );
    }

    var lookRot = Quaternion.LookRotation(Vector3.forward, vectorToTarget);
    var rotSlerp = Time.smoothDeltaTime * rotationSpeed * rotationAdjustment;
    headTransform.rotation = Quaternion.Slerp(headTransform.rotation, lookRot, rotSlerp);

    //Add position to the list only if we've moved far enough since the last one to make it more framerate independent on how many points we need
    if(Vector2.Distance(headTransform.position, positions.Get(0)) > minDistance / 6f){
      positions.Add(headTransform.position);
    }

    for(int i = 1; i < snakeMembers.Count; i++){
      var curBodyPart = snakeMembers[i].transform;
      var prevBodyPart = snakeMembers[i - 1].transform;
      var prevPartPosition = (Vector2)prevBodyPart.position;

      var savedPosition = positions.GetNextInLine((i - 1) * minDistance);

      if(savedPosition != Vector2.zero){

        //Apply a speed change adjustment to try to settle into the ideal distance position
        //move faster when you're farther away from the ideal distance, slower when you're closer
        var dis = Vector3.Distance(prevPartPosition, curBodyPart.position);
        var pctOfIdealDistance = dis / minDistance;

        var adjustmentAmount = 0.65f;
        var speedAdjustment = (pctOfIdealDistance * adjustmentAmount) + (1 - adjustmentAmount);

        var savedPositionDirection = (savedPosition - (Vector2)curBodyPart.position).normalized;
        curBodyPart.rotation = Quaternion.LookRotation(Vector3.forward, savedPositionDirection);
        curBodyPart.Translate(curBodyPart.up * moveSpeed * speedAdjustment * Time.smoothDeltaTime, Space.World);

      }else{
        var dis = Vector3.Distance(prevPartPosition, curBodyPart.position);

        var lerpAmt = Time.smoothDeltaTime * dis / minDistance * moveSpeed;

        if(lerpAmt > 0.5f){ lerpAmt = 0.5f; }

        curBodyPart.position = Vector3.Slerp(curBodyPart.position, prevPartPosition, lerpAmt);
        curBodyPart.rotation = Quaternion.Slerp(curBodyPart.rotation, prevBodyPart.rotation, 0.4f);
      }

    }
  }


  public void SetIsBoosting(bool isBoosting){
    this.isBoosting = isBoosting;
  }

  public Bounds CalculateBounds(){
    float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;

    for(int i = 0; i < snakeMembers.Count; i++)
    {
      var partPos = snakeMembers[i].transform.position;

      if(partPos.x < minX){
        minX = partPos.x;
      }
      if(partPos.x > maxX){
        maxX = partPos.x;
      }

      if(partPos.y < minY){
        minY = partPos.y;
      }
      if(partPos.y > maxY){
        maxY = partPos.y;
      }
    }

    var middle = new Vector2(minX + (maxX - minX) / 2f, minY + (maxY - minY) / 2f);
    var size = new Vector2(maxX - minX, maxY - minY);

    return new Bounds(middle, size);
  }

  public void FloaterToSnakePart(GameObject go, Snake snake, Transform newParent){
    var randMove = go.GetComponent<RandomMovement>();
    var rb = go.GetComponent<Rigidbody2D>();

    randMove.enabled = false;

    rb.isKinematic = true;
    rb.Sleep();

    go.transform.SetParent(newParent, true);

    var atomRenderer = go.GetComponentInChildren<AtomRenderer>();

    freeAtoms.Remove(atomRenderer);

    var snakeSegment = go.gameObject.GetComponent<SnakeSegment>();
    snakeSegment.snake = snake;
    snakeSegment.enabled = true;
  }

  public void SnakePartToFloater(GameObject go, Transform newParent){
    var randMove = go.GetComponent<RandomMovement>();
    var rb = go.GetComponent<Rigidbody2D>();

    randMove.enabled = true;

    rb.isKinematic = false;
    rb.WakeUp();

    go.transform.SetParent(newParent, true);

    var atomRenderer = go.GetComponentInChildren<AtomRenderer>();

    freeAtoms.Add(atomRenderer);

    var snakeSegment = go.gameObject.GetComponent<SnakeSegment>();
    snakeSegment.snake = null;
    snakeSegment.enabled = false;
  }

  Color HeadColor(int size){
    var atomColor = AtomRenderer.ColorMap(size);
    // return new Color(atomColor.r, atomColor.g, atomColor.b, 0.7f);
    if(size < 10){
      atomColor = Color.white;
    }
    return atomColor.SetA(0.8f);
  }

}
