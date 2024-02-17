using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false) ]
class SingletonAttribute : Attribute { }

//Same as above, but should only get instanciated for the game instance and destroyed afterwards
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false) ]
class GameSingletonAttribute : Attribute { }
