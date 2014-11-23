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

            DateTime startTime2 = DateTime.Now;
            int numNodesVisited2 = ParallelBFS(graph);
            DateTime endTime2 = DateTime.Now;

            var timediff2 = endTime2 - startTime2;
            Console.WriteLine("Visited: " + numNodesVisited2 + " nodes.");
            Console.WriteLine("Time to finish execution: " + timediff2);

            DateTime startTime3 = DateTime.Now;
            int numNodesVisited3 = BFSLevels(graph);
            DateTime endTime3 = DateTime.Now;

            var timediff3 = endTime3 - startTime3;
            Console.WriteLine("Visited: " + numNodesVisited3 + " nodes.");
            Console.WriteLine("Time to finish execution: " + timediff3);
            
            
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

                //Assign thread ID to each vertex
                for (int j = startIndex; j < numSubgraphVertices; j++)
                {
                    graphAsList[j].threadID = threadNum;
                }
                List<IVertex> subGraph = graphAsList.GetRange(startIndex, numSubgraphVertices);

                partition.Add(new OneDimensionalPartition(subGraph, threadNum));
                threadList.Add(new Thread(new ThreadStart(partition[threadNum].bfs)));
                //BFSOnOneDimensionalPartitioning(subGraph, i);
            }

            //Parallel.ForEach<
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
            else if(child != edge.Vertex1)
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

        public static int BFSLevels(IGraph graph)
        {
            List<IVertex> current = new List<IVertex>();
            ConcurrentBag<IVertex> next = new ConcurrentBag<IVertex>();
            int numVisitedNodes = 0;
            uint level = 0;

            Parallel.ForEach(graph.Vertices, vertex =>
            {
                vertex.Visited = false;
                vertex.Level = 0;
            });

            IVertex root = graph.Vertices.FirstOrDefault();
            next.Add(root);
            root.Level = 0;
            numVisitedNodes++;

            while (next.Where(node => (node != null && node.Level == level)).Count() > 0)
            {
                current = next.Where(node => (node != null && node.Level == level)).ToList();
                Parallel.ForEach(current, currentNode =>
                {
                    Parallel.ForEach(currentNode.OutgoingEdges, edge =>
                    {
                        IVertex child = null;
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
                                next.Add(child);
                                child.Visited = true; ;
                                child.Level = level + 1;
                                numVisitedNodes++;
                            }
                        }
                    });
                });
                level++;
            }
            return numVisitedNodes;
        }
    }
}
