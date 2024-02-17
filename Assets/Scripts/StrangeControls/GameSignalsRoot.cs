using strange.extensions.context.impl;

public class GameSignalsRoot : ContextView
{
    public string startSignalName;

    void Awake()
    {
        context = new GameSignalsContext(this);
    }
}

