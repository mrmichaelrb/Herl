using UnityEngine;

public class GameObjectPool<T>
{
  readonly int _count;
  readonly GameObject[] _gameObjects;
  readonly T[] _components;
  int _lastAvailableIndex = -1;

  int Count
  {
    get
    {
      return _count;
    }
  }

  private GameObjectPool()
  {
  }

  public GameObjectPool(int count, GameObject prefab)
  {
    prefab.SetActive(false);

    _count = count;
    _gameObjects = new GameObject[_count];
    _components = new T[_count];

    for (int i = 0; i < _count; i++)
    {
      GameObject newObject = GameObject.Instantiate(prefab);
      _gameObjects[i] = newObject;
      _components[i] = newObject.GetComponent<T>();
    }
  }

  private int GetAvailableIndex()
  {
    int currentIndex = (_lastAvailableIndex + 1) % _count;

    while (currentIndex != _lastAvailableIndex)
    {
      GameObject currentObject = _gameObjects[currentIndex];

      if (!currentObject.activeSelf)
      {
        _lastAvailableIndex = currentIndex;
        return currentIndex;
      }

      currentIndex = (currentIndex + 1) % _count;
    }

    return -1;
  }

  public GameObject GetAvailableGameObject()
  {
    int index = GetAvailableIndex();

    if (index < 0)
    {
      return null;
    }
    else
    {
      return _gameObjects[index];
    }
  }

  public T GetAvailableComponent()
  {
    int index = GetAvailableIndex();

    if (index < 0)
    {
      // Returns null if a normal reference type
      return default(T);
    }
    else
    {
      return _components[index];
    }
  }

  public GameObject GetGameObject(int index)
  {
    return _gameObjects[index];
  }

  public T GetComponent(int index)
  {
    return _components[index];
  }

  public T[] GetEnabledComponents()
  {
    int enabledCount = 0;

    for (int i = 0; i < _count; i++)
    {
      if (_gameObjects[i].activeSelf)
      {
        enabledCount++;
      }
    }

    int enabledIndex = 0;
    T[] results = new T[enabledCount];

    if (enabledCount > 0)
    {
      for (int i = 0; i < _count; i++)
      {
        if (_gameObjects[i].activeSelf)
        {
          results[enabledIndex] = _components[i];
          enabledIndex++;
        }
      }
    }

    return results;
  }
}
