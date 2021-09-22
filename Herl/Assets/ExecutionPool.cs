using System;
using System.Collections.Generic;

public class ExecutionPool
{
  const int NoIndex = -1;

  public class Registration : IDisposable
  {
    ExecutionPool _pool;
    int _index = NoIndex;

    public Registration(ExecutionPool pool, int index)
    {
      _pool = pool;
      _index = index;
    }

    ~Registration()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (_index != NoIndex)
      {
        _pool.Unregister(_index);
        _pool = null;
        _index = NoIndex;
      }
    }

    public bool IsScheduled
    {
      get
      {
        return _pool.IsScheduled(_index);
      }
    }
  }

  static List<ExecutionPool> s_pools = new List<ExecutionPool>();

  int _requestsPerFrame;
  List<bool> _registered = new List<bool>();
  int _count;
  int _minIndex;
  int _maxIndex;

  public static void NextFrame()
  {
    foreach(ExecutionPool pool in s_pools)
    {
      pool.InternalNextFrame();
    }
  }

  public ExecutionPool(int requestsPerFrame)
  {
    _requestsPerFrame = requestsPerFrame;

    s_pools.Add(this);
  }

  void InternalNextFrame()
  {
    if (_count > 0)
    {
      _minIndex = _maxIndex;

      int requestCount = 0;

      do
      {
        if (_registered[_maxIndex])
        {
          requestCount++;
        }

        _maxIndex = (_maxIndex + 1) % _count;

      } while ((requestCount < _requestsPerFrame) && (_maxIndex != _minIndex));
    }
  }

  public Registration Register()
  {
    int index = NoIndex;

    for (int i = 0; i < _count; i++)
    {
      if (!_registered[i])
      {
        _registered[i] = true;
        index = i;
        break;
      }
    }

    if (index == NoIndex)
    {
      _registered.Add(true);
      _count = _registered.Count;
      index = _count - 1;
    }

    return new Registration(this, index);
  }

  void Unregister(int index)
  {
    _registered[index] = false;
  }

  public bool IsScheduled(int index)
  {
    if (_minIndex < _maxIndex)
    {
      return ((index >= _minIndex) && (index < _maxIndex));
    }
    else
    {
      return ((index >= _minIndex) || (index < _maxIndex));
    }
  }
}
