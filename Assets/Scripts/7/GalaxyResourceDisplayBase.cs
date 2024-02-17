using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System;

public abstract class GalaxyResourceDisplayBase : View {
  public abstract GalaxyResource resource {get; set;}

  public Dictionary<GameResourceType, GameResource> resourceDeltas;
  public Action OnImportExportChanged;

  //For string changer
  public string stringNamespace;

  protected override void Awake () {
    base.Awake();
  }
}