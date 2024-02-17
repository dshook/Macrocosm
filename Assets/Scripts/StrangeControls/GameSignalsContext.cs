using UnityEngine;
using strange.extensions.context.api;
using strange.extensions.context.impl;
using strange.extensions.command.api;
using strange.extensions.command.impl;
using System.Reflection;

public class GameSignalsContext : MVCSContextBase
{
  public GameSignalsContext(MonoBehaviour view) : base(view)
  {
    //Create a throwaway instance of this so it won't get code stripped by some part of the android build process hopefully
    var ed = new strange.extensions.dispatcher.eventdispatcher.impl.EventDispatcher();
    ed.Nonsense();
  }

  // Unbind the default EventCommandBinder and rebind the SignalCommandBinder
  protected override void addCoreComponents()
  {
    base.addCoreComponents();
    injectionBinder.Unbind<ICommandBinder>();
    injectionBinder.Bind<ICommandBinder>().To<SignalCommandBinder>().ToSingleton();
  }

  override public IContext Start()
  {
    UnityEngine.Profiling.Profiler.BeginSample("GameSignalsContext Start");
    var beforeTime = Time.realtimeSinceStartup;
    base.Start();
    DebugExtensions.DebugWithTime("Game Signals Start", beforeTime);
    UnityEngine.Profiling.Profiler.EndSample();

    return this;
  }

  protected override void mapBindings()
  {
    var beforeTime = Time.realtimeSinceStartup;
    var assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
    BindSingletons(assemblyTypes, typeof(SingletonAttribute), false);

    //Manually create the game data model so we can have it injected
    var gameDataModel = new GameDataModel();
    injectionBinder.injector.Inject(gameDataModel);
    injectionBinder.Bind<GameDataModel>().To(gameDataModel);
    injectionBinder.Bind<PaletteService>().To<PaletteService>().ToSingleton();

    BindViews(assemblyTypes);
    BindSignals(assemblyTypes);

    var gameSystems = GameObject.Find("GameSystems");
    var inputService = gameSystems.GetComponent<InputService>();
    var gameSaver = gameSystems.GetComponent<GameSaverService>();
    var transitionService = gameSystems.GetComponent<StageTransitionService>();
    var objectPool = gameSystems.GetComponent<ObjectPool>();
    var cameraService = gameSystems.GetComponent<CameraService>();

    var canvasGo = GameObject.Find("Camera Canvas");
    var canvas = canvasGo.GetComponent<Canvas>();

    var audioSystem = GameObject.Find("AudioSystem").GetComponent<AudioService>();

    injectionBinder.Bind<ResourceLoaderService>().To<ResourceLoaderService>().ToSingleton();
    injectionBinder.Bind<AudioService>().To(audioSystem).ToSingleton();
    injectionBinder.Bind<InputService>().To(inputService).ToSingleton();
    injectionBinder.Bind<SpawnService>().To<SpawnService>().ToSingleton();
    injectionBinder.Bind<StringChanger>().To<StringChanger>().ToSingleton();
    injectionBinder.Bind<StageTransitionService>().To(transitionService).ToSingleton();
    injectionBinder.Bind<StageRulesService>().To<StageRules>().ToSingleton();
    injectionBinder.Bind<CameraService>().To(cameraService).ToSingleton();
    injectionBinder.Bind<GameSaverService>().To(gameSaver).ToSingleton();
    injectionBinder.Bind<TimeService>().To<TimeService>().ToSingleton();
    injectionBinder.Bind<ObjectPool>().To(objectPool).ToSingleton();
    injectionBinder.Bind<Canvas>().To(canvas).ToSingleton();

    var floatingText = gameSystems.GetComponent<FloatingText>();
    var tutorialSystem = gameSystems.GetComponent<TutorialSystem>();

    injectionBinder.Bind<FloatingText>().To(floatingText).ToSingleton();
    injectionBinder.Bind<TutorialSystem>().To(tutorialSystem).ToSingleton();

    injectionBinder.Bind<GalaxyRouteCache>().To<GalaxyRouteCache>().ToSingleton();

    //For lack of a better place to put it now
    LeanTween.init( 400 );
    DebugExtensions.DebugWithTime("Bindings mapped", beforeTime);
  }
}

