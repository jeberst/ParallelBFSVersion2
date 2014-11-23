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
    class Program
    {
        const int NUM_THREADS = 8;
        [STAThread]
        static void Main(string[] args)
        {
            GraphGenerator graphGenerator = new GraphGenerator();
            IGraph graph = graphGenerator.Generator(true);



            var degreeZero = graph.Vertices.Where( a => a.Degree == 0);

            foreach(IVertex vertex in degreeZero.ToList())
            {
                graph.Vertices.Remove(vertex);
            }

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
            resetGraph(graph);

            startTime = DateTime.Now;
            numNodesVisited = ParallelBFS(graph);
            endTime = DateTime.Now;

            timediff = endTime - startTime;
            Console.WriteLine("Visited: " + numNodesVisited + " nodes.");
            Console.WriteLine("Time to finish execution: " + timediff);
            resetGraph(graph);

            startTime = DateTime.Now;
            numNodesVisited = OneDimensionalPartitioning(graph);
            endTime = DateTime.Now;
            timediff = endTime - startTime;
            Console.WriteLine("Visited: " + numNodesVisited + " nodes.");
            Console.WriteLine("Time to finish execution: " + timediff);
            resetGraph(graph);

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }



        
        static int OneDimensionalPartitioning(IGraph g)
        {
            //Divide graph into portions and call
            //threads on BFSOnOneDimensionalPartitioning();
            int graphSize = g.Vertices.Count();
            int numSubgraphVertices = graphSize / NUM_THREADS;
            g.Vertices.FirstOrDefault().Level = 0;

            List<Thread> threadList = new List<Thread>();
            List<OneDimensionalPartitionQueue> partition = new List<OneDimensionalPartitionQueue>();

            for (int threadNum = 0; threadNum < NUM_THREADS; threadNum++)
            {
                int remainder = graphSize % NUM_THREADS;
                List<IVertex> threadVertices = new List<IVertex>();

                int startIndex = threadNum * numSubgraphVertices;

                List<IVertex> graphAsList = g.Vertices.Cast<IVertex>().ToList();

                if (threadNum == NUM_THREADS - 1)
                {
                    numSubgraphVertices += remainder;
                }
                List<IVertex> subGraph = graphAsList.GetRange(startIndex, numSubgraphVertices);

                partition.Add(new OneDimensionalPartitionQueue(subGraph, threadNum));
                threadList.Add(new Thread(new ThreadStart(partition[threadNum].bfs)));
            }

            for (int i = 0; i < threadList.Count; i++)
            {
                //Console.WriteLine("Starting thread " + i);
                threadList[i].Start();
            }
            int totalCounted = 0;
            for (int i = 0; i < threadList.Count; i++)
            {
                threadList[i].Join();
                //Console.WriteLine("Thread " + i + " finished");
                totalCounted += partition[i].numItemsEnqueued;
            }

            return totalCounted;
        }

        static void resetGraph(IGraph g)
        {
            foreach (IVertex v in g.Vertices)
            {
                v.Visited = false;
                v.Level = UInt32.MaxValue;
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
            int duplicateConnection = 0;
            int numberofEdges = 0;

            root.Visited = true;
            queue.Enqueue(root);
         
            numVisitedNodes++;

            while (queue.Count > 0)
            {
                IVertex currentNode = queue.Dequeue();
             

                foreach (IEdge edge in currentNode.OutgoingEdges)
                {
                    IVertex child;
                    numberofEdges++;

                    if (currentNode != edge.Vertex2)
                    {
                        child = edge.Vertex2;
                    }
                    else
                    {
                        child = edge.Vertex1;
                    }

                    if (!child.Visited && child != null)
                    {
                        child.Visited = true;
                        numVisitedNodes++;
                        queue.Enqueue(child);
                    }
                    else
                    {
                        duplicateConnection++;
                    }
                }
            }

            var unVisited = graph.Vertices.Where(a => a.Visited == false);
            return numVisitedNodes;
        }

        static int ParallelBFS(IGraph graph)
        {
           // int[] distances = new int[graph.Vertices.Count];
            int numVisitedNodes = 0;
            IVertex root = graph.Vertices.FirstOrDefault();
             ConcurrentQueue<IVertex> queue = new ConcurrentQueue<IVertex>();

             Parallel.ForEach(graph.Vertices, vertex =>
                 {
                     vertex.Visited = false;
                 }
                 );

                    root.Visited = true;
                    numVisitedNodes++;

                    queue.Enqueue(root);
                   
                    while(!queue.IsEmpty)
                    {
                        numVisitedNodes += queue.AsParallel().Sum(node => parallelDequeue(queue));
                    }

            return numVisitedNodes;
        }

        private static int parallelDequeue(ConcurrentQueue<IVertex> queue)
        {
            int visited = 0;
            IVertex currentNode = null;

            queue.TryDequeue(out currentNode);

            if (currentNode != null)
            {
                List<IEdge> edges = currentNode.OutgoingEdges.ToList();

                visited = edges.AsParallel().Sum(edge => processEdges(edge, currentNode, queue));
            }
            return visited;
        }


        static int processEdges(IEdge edge, IVertex currentNode, ConcurrentQueue<IVertex> queue)
        {
            IVertex child = null;
            int visited = 0;
            if (currentNode != edge.Vertex2)
            {
                child = edge.Vertex2;
            }
            else if (currentNode != edge.Vertex1)
            {
                child = edge.Vertex1;
            }

            if (child != null)
            {
                if (!child.Visited)
                {
                    child.Visited = true;
                    visited++;
                    queue.Enqueue(child);
                }
            }

            return visited;
        }
    }
}
