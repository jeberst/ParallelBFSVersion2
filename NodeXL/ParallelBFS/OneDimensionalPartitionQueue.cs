using System;
using Smrf.NodeXL.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace ParallelBFS
{
    public class OneDimensionalPartitionQueue
    {
        static int finishMask = 0;  //Return state
        static int finishField = 0; //Current status
        static ConcurrentQueue<IVertex>[] localQueue = new ConcurrentQueue<IVertex>[NUM_THREADS];
        const uint NUM_THREADS = 2;
        public int numItemsEnqueued = 0;
        static object lockObject = new object();

        int myThreadID = int.MaxValue;
        List<IVertex> subGraph = null;

        public OneDimensionalPartitionQueue(List<IVertex> subGraph, int threadID)
        {
            //finishMask should ONLY be modifed during constuction.
            finishMask |= 1 << threadID;
            //Console.WriteLine("finishMask = " + finishMask);
            this.myThreadID = threadID;
            this.subGraph = subGraph;
            for (int i = 0; i < this.subGraph.Count(); i++)
            {
                this.subGraph[i].threadID = threadID;
            }
            localQueue[threadID] = new ConcurrentQueue<IVertex>();
        }

        //This version of the function does not look for any particular node
        // and attempts to search the entire graph.
        public void bfs()
        {
            //Set initial list.
            foreach (IVertex v in subGraph)
            {
                if (!v.Visited && v.Level == 0) //Line 3
                {
                    localQueue[v.threadID].Enqueue(v);
                    finishField |= 1 << v.threadID;
                    Interlocked.Increment(ref numItemsEnqueued);
                    v.Visited = true;
                }
            }

            while (finishField != finishMask) // Line 4 // Until everyone marked as finished
            {
                while (localQueue[myThreadID].Count() != 0) // Whlie there are objects in local queue
                {
                    IVertex sourceVertex;
                    bool success = true;
                    do // Until dequeue succeeds
                    {
                        success = localQueue[myThreadID].TryDequeue(out sourceVertex);
                    } while (!success);

                    foreach (IEdge e in sourceVertex.OutgoingEdges)
                    {
                        //Change this flag to decide whether to update all child node depths or not.
                        bool updateChildren = false;
                        IVertex destVertex = null;
                        if (sourceVertex != e.Vertex2)
                        {
                            destVertex = e.Vertex2;
                        }
                        else if (sourceVertex != e.Vertex1)
                        {
                            destVertex = e.Vertex1;
                        }

                        if (destVertex != null && updateChildren)
                        {
                            if (destVertex.Level < sourceVertex.Level + 1 || destVertex.Level == UInt32.MaxValue)
                            {
                                finishField = 0;
                                destVertex.Level = sourceVertex.Level + 1;
                                localQueue[sourceVertex.threadID].Enqueue(sourceVertex);
                                Interlocked.Increment(ref numItemsEnqueued);
                            }
                        }
                        else if (destVertex != null)
                        {
                            if (destVertex.Level == UInt32.MaxValue)
                            {
                                finishField = 0;
                                destVertex.Level = sourceVertex.Level + 1;
                                localQueue[destVertex.threadID].Enqueue(destVertex);
                                Interlocked.Increment(ref numItemsEnqueued);
                            }
                        }
                    }
                }

                lock (lockObject)
                {
                    Interlocked.Exchange(ref finishField, finishField | 1 << myThreadID);
                }
            } 
            //Console.WriteLine(myThreadID + " exiting");
        }
    }
}
