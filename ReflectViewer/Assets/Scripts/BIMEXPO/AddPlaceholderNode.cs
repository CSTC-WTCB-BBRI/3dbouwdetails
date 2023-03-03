using System;
using Unity.Reflect;
using UnityEngine;
using UnityEngine.Reflect;
using UnityEngine.Reflect.Pipeline;


public class AddPlaceholderNode : ReflectNode<EnableRoomPlaceholder>
{
    public GameObjectInput input = new GameObjectInput();

    protected override EnableRoomPlaceholder Create(ReflectBootstrapper hook, ISyncModelProvider provider, IExposedPropertyTable resolver)
    {
        var node = new EnableRoomPlaceholder();
        input.streamEvent = node.OnGameObjectEvent;
        return node;
    }
}

public class EnableRoomPlaceholder : IReflectNodeProcessor
{
    public void OnGameObjectEvent(SyncedData<GameObject> stream, StreamEvent streamEvent)
    {
        if (streamEvent == StreamEvent.Added)
        {
            var gameObject = stream.data;
            if (!gameObject.TryGetComponent(out Metadata meta))
            {
                return;
            }
            if (meta.GetParameter("Comments") == "BIMEXPOPH")
            {
                gameObject.SetActive(true);
            }
        }
    }

    public void OnPipelineInitialized()
    {
        // not needed
    }

    public void OnPipelineShutdown()
    {
        // not needed
    }
}
