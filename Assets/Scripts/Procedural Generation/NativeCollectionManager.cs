using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Centralized manager for native collections lifecycle.
/// Handles creation, tracking, and safe disposal of all native arrays/lists.
/// </summary>
public class NativeCollectionManager : IDisposable
{
    private readonly List<IDisposable> trackedCollections = new();
    private readonly List<JobHandle> activeJobs = new();
    private bool isDisposed = false;

    // Track a native collection for automatic disposal
    public T Track<T>(T collection) where T : IDisposable
    {
        if (isDisposed)
        {
            throw new InvalidOperationException("Cannot track collections on a disposed manager");
        }
        trackedCollections.Add(collection);
        return collection;
    }

    // Track a job handle
    public JobHandle TrackJob(JobHandle job)
    {
        if (isDisposed) return job;
        activeJobs.Add(job);
        return job;
    }

    // Complete all tracked jobs
    public void CompleteAllJobs()
    {
        foreach (var job in activeJobs)
        {
            if (!job.IsCompleted)
            {
                job.Complete();
            }
        }
        activeJobs.Clear();
    }

    // Safely try to access a native collection
    public bool TryAccess<T>(NativeArray<T> array, out int length) where T : struct
    {
        length = 0;
        if (isDisposed) return false;
        
        try
        {
            if (!array.IsCreated) return false;
            length = array.Length;
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    public bool TryAccess<T>(NativeList<T> list, out int length) where T : unmanaged
    {
        length = 0;
        if (isDisposed) return false;
        
        try
        {
            if (!list.IsCreated) return false;
            length = list.Length;
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    // Try to get an element from a native list
    public bool TryGetElement<T>(NativeList<T> list, int index, out T element) where T : unmanaged
    {
        element = default;
        if (isDisposed) return false;
        
        try
        {
            if (!list.IsCreated || index >= list.Length) return false;
            element = list[index];
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    // Dispose a specific collection early (removes from tracking)
    public void DisposeEarly(IDisposable collection)
    {
        if (isDisposed) return;
        
        try
        {
            collection?.Dispose();
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        finally
        {
            trackedCollections.Remove(collection);
        }
    }

    // Dispose everything
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;

        // Complete any outstanding jobs first
        CompleteAllJobs();

        // Dispose all tracked collections
        foreach (var collection in trackedCollections)
        {
            try
            {
                collection?.Dispose();
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error disposing native collection: {ex.Message}");
            }
        }

        trackedCollections.Clear();
        activeJobs.Clear();
    }

    public bool IsDisposed => isDisposed;
}

/// <summary>
/// Helper extensions for cleaner syntax
/// </summary>
public static class NativeCollectionManagerExtensions
{
    public static NativeArray<T> CreateTracked<T>(
        this NativeCollectionManager manager,
        int length,
        Allocator allocator) where T : struct
    {
        var array = new NativeArray<T>(length, allocator);
        return manager.Track(array);
    }

    public static NativeList<T> CreateTrackedList<T>(
        this NativeCollectionManager manager,
        Allocator allocator) where T : unmanaged
    {
        var list = new NativeList<T>(allocator);
        return manager.Track(list);
    }

    public static NativeList<T> CreateTrackedList<T>(
        this NativeCollectionManager manager,
        int initialCapacity,
        Allocator allocator) where T : unmanaged
    {
        var list = new NativeList<T>(initialCapacity, allocator);
        return manager.Track(list);
    }
}