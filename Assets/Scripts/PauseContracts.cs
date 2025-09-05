using UnityEngine;

public interface IPausable
{
    void OnPause();
    void OnResume();
}

/// Marker: components that MUST keep updating during pause (UI, selection, command UIs, camera tweens, etc.)
public interface IRunWhenPaused { }
