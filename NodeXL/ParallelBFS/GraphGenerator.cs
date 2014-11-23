using Smrf.NodeXL.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Smrf.NodeXL.Layouts;
//using Smrf.NodeXL.Algorithms;
using Smrf.NodeXL.GraphDataProviders.Facebook;
using System.Threading;
using System.IO;
using System.Xml;
using Smrf.NodeXL.Adapters;

namespace ParallelBFS
{
    public class GraphGenerator
    {
        public GraphGenerator() { }

        public IGraph Generator(bool facebook)
        {
            GraphMLGraphAdapter graphMlAdapter = new Smrf.NodeXL.Adapters.GraphMLGraphAdapter();
             IGraph graph;
            if (facebook)
            {
                graph = graphMlAdapter.LoadGraphFromFile("facebookgraph.graphml");
            }
            else
            {
                graph = graphMlAdapter.LoadGraphFromFile("sndata.graphml");
            }
            
           
            return graph;
        }

        private void writeFile()
        {
            Smrf.NodeXL.GraphDataProviders.Facebook.FacebookGraphDataProvider fbGraph = new Smrf.NodeXL.GraphDataProviders.Facebook.FacebookGraphDataProvider();
            string data = "";

            try
            {
                fbGraph.TryGetGraphDataAsTemporaryFile(out data);
                //fbGraph.TryGetGraphData(out data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n Please copy the following information and paste it to http://socialnetimporter.codeplex.com/discussions :\n" + e.StackTrace);
            }
            TextWriter tw = new StreamWriter("GraphASGraphML.txt");


            // write a line of text to the file
            tw.WriteLine(data);

            // close the stream
            tw.Close();
        }

        public void saveGraph(IGraph graph)
        {
            GraphMLGraphAdapter graphMlAdapter = new Smrf.NodeXL.Adapters.GraphMLGraphAdapter();

          FileStream fs = File.Create("graph.graphml");
          graphMlAdapter.SaveGraph(graph, fs);
        }
    }
}
