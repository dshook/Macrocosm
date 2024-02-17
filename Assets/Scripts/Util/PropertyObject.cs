using System;

public class PropertyObject<T>
{
  public Action<T> OnChange;

  private T _value;

  public PropertyObject(T init){
    _value = init;
  }

  public T value {
    get{
      return _value;
    }
    set{
      _value = value;
      if(OnChange != null){
        OnChange(_value);
      }
    }
  }
}