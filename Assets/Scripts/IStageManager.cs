public interface IStageManager
{
  //stage is being setup for the first time or going to a different substage
  void Init(bool isInitialCall);

  void OnTransitionTo(StageTransitionData data);

  void OnTransitionAway(bool toMenu);

  //for reset and removal of junk
  void Cleanup();
}