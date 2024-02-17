using UnityEngine;
using System.Collections;
using strange.extensions.mediation.impl;

public class RequestReviewView : View
{
  [Inject] StageTransitionEndSignal transitionEndSignal {get; set;}
  [Inject] GameDataModel gameData {get; set;}

  protected override void Awake() {
    base.Awake();

    transitionEndSignal.AddListener(OnTransitionEnd);
  }

  void OnTransitionEnd(StageTransitionData data){
    if(data.stage == 6 && !gameData.requestedReview){
      RequestReview();
    }
  }

  void RequestReview(){
    gameData.requestedReview = true;
    Debug.Log("Requesting Review");

#if UNITY_IOS
    OpenReviewIOS();
#elif UNITY_ANDROID
    StartCoroutine(OpenReviewAndroid());
#endif

  }

#if UNITY_IOS
  void OpenReviewIOS(){
    UnityEngine.iOS.Device.RequestStoreReview();
  }
#endif


#if UNITY_ANDROID
  IEnumerator OpenReviewAndroid(){
    var _reviewManager = new Google.Play.Review.ReviewManager();

    var requestFlowOperation = _reviewManager.RequestReviewFlow();
    yield return requestFlowOperation;
    if (requestFlowOperation.Error != Google.Play.Review.ReviewErrorCode.NoError)
    {
        Debug.LogError("Android error review request: " + requestFlowOperation.Error.ToString());
        yield break;
    }

    var _playReviewInfo = requestFlowOperation.GetResult();
    var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
    yield return launchFlowOperation;

    _playReviewInfo = null; // Reset the object
    if (launchFlowOperation.Error != Google.Play.Review.ReviewErrorCode.NoError)
    {
        Debug.LogError("Android error launch flow: " + launchFlowOperation.Error.ToString());
        yield break;
    }
    // The flow has finished. The API does not indicate whether the user
    // reviewed or not, or even whether the review dialog was shown. Thus, no
    // matter the result, we continue our app flow.
  }
#endif

}