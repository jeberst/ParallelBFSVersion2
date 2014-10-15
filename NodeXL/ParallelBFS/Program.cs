using System;
using Smrf.NodeXL.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ParallelBFS
{
    class Program
    {
        const int NUM_THREADS = 8;
        [STAThread]
        static void Main(string[] args)
        {
            GraphGenerator graphGenerator = new GraphGenerator();
            IGraph graph = graphGenerator.Generator(true);

            //IVertex discoveredVertex = BreadthFirstSearch(graph, "Last Name", "Pecoraro");
            //if (discoveredVertex != null)
            //{
            //    string firstName = discoveredVertex.GetValue("First Name").ToString();
            //    string lastName = discoveredVertex.GetValue("Last Name").ToString();
            //}
            
            // Testing BFS implementation that iterates through all nodes
            DateTime startTime = DateTime.Now;
            int numNodesVisited = BreadthFirstSearch(graph);
            DateTime endTime = DateTime.Now;

            var timediff = endTime - startTime;
            Console.WriteLine("Visited: " + numNodesVisited + " nodes.");
            Console.WriteLine("Time to finish execution: " + timediff);
            
            
            OneDimensionalPartitioning(graph);
        }

        
        static void OneDimensionalPartitioning(IGraph g)
        {
            //Divide graph into portions and call
            //threads on BFSOnOneDimensionalPartitioning();
            int graphSize = g.Vertices.Count();
            int numSubgraphVertices = graphSize / NUM_THREADS;
            g.Vertices.FirstOrDefault().Level = 0;

            List<Thread> threadList = new List<Thread>();
            List<OneDimensionalPartition> partition = new List<OneDimensionalPartition>();

            for (int i = 0; i < NUM_THREADS; i++ )
            {
            }
            //Parallel.ForEach

            for (int i = 0; i < NUM_THREADS; i++)
            {
                int remainder = graphSize % NUM_THREADS;
                List<IVertex> threadVertices = new List<IVertex>();

                int startIndex = i * numSubgraphVertices;

                List<IVertex> graphAsList = g.Vertices.Cast<IVertex>().ToList();

                if (i == NUM_THREADS - 1)
                {
                    numSubgraphVertices += remainder;
                }

                List<IVertex> subGraph = graphAsList.GetRange(startIndex, numSubgraphVertices);
                
                partition.Add(new OneDimensionalPartition(subGraph, i));
                threadList.Add(new Thread(new ThreadStart(partition[i].bfs)));
                //BFSOnOneDimensionalPartitioning(subGraph, i);
            }

            //Parallel.ForEach<
        }
        
        //static void BFSOnOneDimensionalPartitioning(IGraph subGraph)
        static void BFSOnOneDimensionalPartitioning(List<IVertex> subGraph, int myThreadID )
        {
            // L = level of vertices serched

            //Lva = (L sub (v sub s))
            //                     / 0, v = (v sub s), where (v sub s) is a source
            //1: Initilize Lva(v) <
            //                     \ inf, otherwise
            // 2: for l = 0 to inf do
            // 3:   F <- {v|Lva(v) = l}, the set of all local vertices with level l
            // 4:   if F = null for all processors then
            // 5:       Terminate main loop
            // 6:   end if
            // 7:   N <- {neighbors of vertices in f (not neccessarily local
            // 8:   for all processors q do
            // 9:       Nq <- { vertices in N owned by processor q}
            //10:       Send Nq to processor q
            //11:       Receive N`q from processor q
            //12:   end for
            //13:   N` <- Uq N'q ( Ther N`q may overlap)
            //14:   for v elementOf N` and Lvs(v) = inf do
            //15:       Lvs(v) <- l + 1
            //16:   endfor
            //17: end for


            for (UInt32 currentLevel = 0; currentLevel < UInt32.MaxValue; currentLevel++ ) // Line 2
            {
                List<IVertex> F = new List<IVertex>();
                foreach (IVertex v in subGraph)
                {
                    if ( !v.Visited && v.Level == currentLevel ) //Line 3
                    {
                        F.Add(v);  // Line3
                    }

                    if ( F.Count == 0 ) // Line 4
                    {
                        //TODO: Check other threads if done
                        //send(We are done?);
                        return;  // Line 5
                    }
                }

                //Line 7
                List<IVertex> N = new List<IVertex>();
                foreach (IVertex v in F)
                {
                    foreach (IEdge e in v.OutgoingEdges)
                    {
                        N.Add(e.Vertex2);
                    }
                }

                //Lines 8 - 12
                List<IVertex> newNeighbors = new List<IVertex>();
                foreach (IVertex v in N)
                {
                    for ( int i = 0; i < NUM_THREADS; i++ )
                    {
                        List< List<IVertex> >Nq = new List< List <IVertex>>();
                        if ( v.threadID == i )
                        {
                            Nq[i].Add(v);
                        }
                    }
                    for (int i = 0; i < NUM_THREADS; i++)
                    {
                        if (i != myThreadID)
                        {
                            //TODO: Implement send
                            //send(thread[i], Nq[i]);
                        }
                    }
                    for (int i = 0; i < NUM_THREADS; i++)
                    {
                        List<IVertex> recieved;
                        if (i != myThreadID)
                        {
                            //TODO: Implement recieve
                            //recieved = recieve(thread[i]);
                            //newNeighbors.AddRange(recieved);
                        }
                    }
                }

                N.AddRange(newNeighbors); // Line 13
                //TODO: Check for repaeats and remove as needed (May not be neccicary)
                foreach ( IVertex v in N)
                {
                    if ( v.Level == UInt32.MaxValue )
                    {
                        v.Level = currentLevel + 1;
                    }
                }
            }
        }


        static Smrf.NodeXL.Core.IVertex BreadthFirstSearch(IGraph graph, string searchKey, string searchValue)
        {
            Queue<IVertex> queue = new Queue<IVertex>();
            IVertex root = graph.Vertices.FirstOrDefault();

            queue.Enqueue(root);
            root.Visited = true;

            while (queue.Count > 0)
            {
                IVertex currentNode = queue.Dequeue();

                string val = currentNode.GetValue(searchKey).ToString();
                if (currentNode.GetValue(searchKey).ToString() == searchValue)
                {
                    return currentNode;
                }

                foreach (IEdge edge in currentNode.IncidentEdges)
                {
                    IVertex child = edge.Vertex2;
                    if (!child.Visited)
                    {
                        queue.Enqueue(child);
                        child.Visited = true;
                    }
                }
            }

            return null;
        }

        // Returns the number of node that were visited
        static int BreadthFirstSearch(IGraph graph)
        {
            Queue<IVertex> queue = new Queue<IVertex>();
            IVertex root = graph.Vertices.FirstOrDefault();
            int numVisitedNodes = 0;

            queue.Enqueue(root);
            root.Visited = true;
            numVisitedNodes++;

            while (queue.Count > 0)
            {
                IVertex currentNode = queue.Dequeue();
                foreach (IEdge edge in currentNode.IncidentEdges)
                {
                    IVertex child;

                    if (currentNode != edge.Vertex2)
                    {
                        child = edge.Vertex2;
                    }
                    else
                    {
                        child = edge.Vertex1;
                    }

                    if (!child.Visited)
                    {
                        queue.Enqueue(child);
                        child.Visited = true;
                        numVisitedNodes++;
                    }
                }
            }

            return numVisitedNodes;
        }
    }


    class OneDimensionalPartition
    {
        const int NUM_THREADS = 8;
        int myThreadID = int.MaxValue;
        List<IVertex> subGraph = null;
        public OneDimensionalPartition(List<IVertex> subGraph, int threadID)
        {
            this.myThreadID = threadID;
            this.subGraph = subGraph;
        }

        //This version of the function does not look for any particular node
        // and attempts to search the entire graph.
        public void bfs()
        {

            // L = level of vertices serched

            //Lva = (L sub (v sub s))
            //                     / 0, v = (v sub s), where (v sub s) is a source
            //1: Initilize Lva(v) <
            //                     \ inf, otherwise
            // 2: for l = 0 to inf do
            // 3:   F <- {v|Lva(v) = l}, the set of all local vertices with level l
            // 4:   if F = null for all processors then
            // 5:       Terminate main loop
            // 6:   end if
            // 7:   N <- {neighbors of vertices in f (not neccessarily local
            // 8:   for all processors q do
            // 9:       Nq <- { vertices in N owned by processor q}
            //10:       Send Nq to processor q
            //11:       Receive N`q from processor q
            //12:   end for
            //13:   N` <- Uq N'q ( Ther N`q may overlap)
            //14:   for v elementOf N` and Lvs(v) = inf do
            //15:       Lvs(v) <- l + 1
            //16:   endfor
            //17: end for


            for (UInt32 currentLevel = 0; currentLevel < UInt32.MaxValue; currentLevel++) // Line 2
            {
                List<IVertex> F = new List<IVertex>();
                foreach (IVertex v in subGraph)
                {
                    if (!v.Visited && v.Level == currentLevel) //Line 3
                    {
                        F.Add(v);  // Line3
                    }

                    if (F.Count == 0) // Line 4
                    {
                        //TODO: Check other threads if done
                        //send(We are done?);
                        return;  // Line 5
                    }
                }

                //Line 7
                List<IVertex> N = new List<IVertex>();
                foreach (IVertex v in F)
                {
                    foreach (IEdge e in v.OutgoingEdges)
                    {
                        N.Add(e.Vertex2);
                    }
                }

                //Lines 8 - 12
                List<IVertex> newNeighbors = new List<IVertex>();
                foreach (IVertex v in N)
                {
                    for (int i = 0; i < NUM_THREADS; i++)
                    {
                        List<List<IVertex>> Nq = new List<List<IVertex>>();
                        if (v.threadID == i)
                        {
                            Nq[i].Add(v);
                        }
                    }
                    for (int i = 0; i < NUM_THREADS; i++)
                    {
                        if (i != myThreadID)
                        {
                            //TODO: Implement send
                            //send(thread[i], Nq[i]);
                        }
                    }
                    for (int i = 0; i < NUM_THREADS; i++)
                    {
                        List<IVertex> recieved;
                        if (i != myThreadID)
                        {
                            //TODO: Implement recieve
                            //recieved = recieve(thread[i]);
                            //newNeighbors.AddRange(recieved);
                        }
                    }
                }

                N.AddRange(newNeighbors); // Line 13
                //TODO: Check for repaeats and remove as needed (May not be neccicary)
                foreach (IVertex v in N)
                {
                    if (v.Level == UInt32.MaxValue)
                    {
                        v.Level = currentLevel + 1;
                    }
                }
            }
        }
    }
}
