using System;
using Smrf.NodeXL.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;

namespace ParallelBFS
{
    class Program
    {
        const int NUM_THREADS = 2;
        static ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = NUM_THREADS };

        [STAThread]
        static void Main(string[] args)
        {
            StreamWriter writer = new StreamWriter("results.txt");
            GraphGenerator graphGenerator = new GraphGenerator();
            IGraph graph = graphGenerator.Generator(false);

            IVertex root = graph.Vertices.OrderByDescending(a => a.Degree).FirstOrDefault();

            //IVertex discoveredVertex = BreadthFirstSearch(graph, "Last Name", "Pecoraro");
            //if (discoveredVertex != null)
            //{
            //    string firstName = discoveredVertex.GetValue("First Name").ToString();
            //    string lastName = discoveredVertex.GetValue("Last Name").ToString();
            //}

             // Testing BFS implementation that iterates through all nodes
            DateTime startTime = DateTime.Now;
            BreadthFirstSearch(graph, root);
            DateTime endTime = DateTime.Now;
            var timediff = endTime - startTime;
            Console.WriteLine("Time to finish execution: " + timediff);
            writer.WriteLine(timediff.Milliseconds + " sequential");

            var firstGraph = graph.Vertices.Where(a => a.Visited == false).ToList();
            Console.WriteLine("Unvisited " + firstGraph.Count());
            resetGraph(graph);

            startTime = DateTime.Now;
            ParallelBFS(graph, root);
            endTime = DateTime.Now;
            timediff = endTime - startTime;
            Console.WriteLine("Time to finish execution: " + timediff);
            writer.WriteLine(timediff.Milliseconds + " MTBFS");
            var secondGraph = graph.Vertices.Where(a => a.Visited == false);
            Console.WriteLine("Unvisited " + secondGraph.Count());
            resetGraph(graph);

            startTime = DateTime.Now;
            BFSLevels(graph, root);
            endTime = DateTime.Now;
            timediff = endTime - startTime;
            Console.WriteLine("Time to finish execution: " + timediff);
            var thirdGraph = graph.Vertices.Where(a => a.Visited == false);
            Console.WriteLine("Unvisited " + thirdGraph.Count());
            writer.WriteLine(timediff.Milliseconds + " LSBFS");
            resetGraph(graph);

            startTime = DateTime.Now;
            OneDimensionalPartitioning(graph, root);
            endTime = DateTime.Now;
            timediff = endTime - startTime;
            Console.WriteLine("Time to finish execution: " + timediff);
            var fourthGraph = graph.Vertices.Where(a => a.Level == UInt32.MaxValue);
            Console.WriteLine("Unvisited " + fourthGraph.Count());
            writer.WriteLine(timediff.Milliseconds + " PPBFS");

            writer.Close();
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();

        }



        
        static void OneDimensionalPartitioning(IGraph g, IVertex root)
        {
            //Divide graph into portions and call
            //threads on BFSOnOneDimensionalPartitioning();
            int graphSize = g.Vertices.Count();
            int numSubgraphVertices = graphSize / NUM_THREADS;
            root.Level = 0;

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

            for (int i = 0; i < threadList.Count; i++)
            {
                threadList[i].Join();
                //Console.WriteLine("Thread " + i + " finished");
                //totalCounted += partition[i].numItemsEnqueued;
            }
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

        
        static void BreadthFirstSearch(IGraph graph, IVertex root)
        {
            Queue<IVertex> queue = new Queue<IVertex>();
            int duplicateConnection = 0;
            int numberofEdges = 0;

            root.Visited = true;
            queue.Enqueue(root); 

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
                        queue.Enqueue(child);
                    }
                    else
                    {
                        duplicateConnection++;
                    }
                }
            }

            var unVisited = graph.Vertices.Where(a => a.Visited == false);
        }

        static void ParallelBFS(IGraph graph, IVertex root)
        {
            int[] distances = new int[graph.Vertices.Count*2];

            BlockingCollection<IVertex> queue = new BlockingCollection<IVertex>();


            Parallel.ForEach(graph.Vertices, parallelOptions, vertex =>
                {
                    vertex.Visited = false;
                    distances[vertex.ID] = -1;
                }
                );

                    root.Visited = true;
                    distances[root.ID] = 0;

                    queue.Add(root);

                    while (queue.Count != 0)
                    {
                         Parallel.ForEach(queue, parallelOptions, node => parallelDequeue(queue, distances));
                    }
        }

        private static void parallelDequeue(BlockingCollection<IVertex> queue, int[] distances)
        {

                IVertex currentNode = null;

                while (currentNode == null)
                {
                    queue.TryTake(out currentNode);
                }


                if (currentNode != null)
                {
                    int[] sums = new int[currentNode.OutgoingEdges.Count * 2];
                    List<IEdge> edges = currentNode.OutgoingEdges.ToList();

                    Parallel.ForEach(edges, parallelOptions, edge => processEdges(edge, currentNode, queue, distances));
            } 
        }


        static void processEdges(IEdge edge, IVertex currentNode, BlockingCollection<IVertex> queue, int[] distances)
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
                    if (distances[child.ID] == -1)
                    {
                        child.Visited = true;
                        distances[child.ID] = distances[currentNode.ID] + 1;
                        queue.Add(child);
                    }
                }
        }

        public static void BFSLevels(IGraph graph, IVertex root)
        {
            List<IVertex> current = new List<IVertex>();
            ConcurrentBag<IVertex> next = new ConcurrentBag<IVertex>();
            uint level = 0;

            Parallel.ForEach(graph.Vertices, parallelOptions, vertex =>
            {
                vertex.Visited = false;
                vertex.Level = 0;
            });

            next.Add(root);
            root.Level = 0;

            while (next.Where(node => (node != null && node.Level == level)).Count() > 0)
            {
                current = next.Where(node => (node != null && node.Level == level)).ToList();
                Parallel.ForEach(current, parallelOptions, currentNode =>
                {
                    Parallel.ForEach(currentNode.OutgoingEdges, parallelOptions, edge =>
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
                            if (child != null && !child.Visited)
                            {
                                next.Add(child);
                                child.Visited = true; ;
                                child.Level = level + 1;
                            }
                    });
                });
                level++;
            }
        }
    }
}
