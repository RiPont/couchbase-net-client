using System;
using System.Collections.Generic;
using System.Linq;

namespace Couchbase.Core.Configuration.Server
{
    internal static class BucketConfigExtensions
    {
        public static List<NodeAdapter> GetNodes(this BucketConfig bucketConfig)
        {
            var nodeAdapters = new List<NodeAdapter>();
            var nodes = bucketConfig.Nodes;
            var nodesExt = bucketConfig.NodesExt;

            if (nodesExt == null)
                nodeAdapters.AddRange(nodes.Select(t => new NodeAdapter(t, null, bucketConfig)));
            else if (nodes.Count == nodesExt.Count)
                nodeAdapters.AddRange(nodes.Select((t, i) => new NodeAdapter(t, nodesExt[i], bucketConfig)));
            else if (nodesExt.Count < nodes.Count)
                nodeAdapters.AddRange(nodes.Select(t => new NodeAdapter(t, null, bucketConfig)));
            else if (nodesExt.Count > nodes.Count)
            {
                //In certain cases the server will return an evicted node at the end of the NodesExt list
                //we filter that list to ensure parity between Nodes and NodesExt - see NCBC-2422 for details

                var temp = new List<NodesExt>();
                foreach (var ext in nodesExt)
                {
                    if (ext.Services.Kv <= 0) continue;
                    if (!nodes.Any(n => n.Hostname.Contains(ext.Hostname)))
                    {
                        temp.Add(ext);
                    }
                }

                //remove the k/v server from nodesExt that was evicted
                foreach (var ext in temp)
                {
                    nodesExt.Remove(ext);
                }

                //create the adapters list - if node exists, it should pair with its equiv nodeExt
                foreach (var ext in nodesExt)
                {
                    var node = nodes.FirstOrDefault(x => x.Hostname.Contains(ext.Hostname));
                    nodeAdapters.Add(new NodeAdapter(node, ext, bucketConfig));
                }
            }
            else
                nodeAdapters.AddRange(nodesExt.Select((t, i) => new NodeAdapter(null, nodesExt[i], bucketConfig)));

            return nodeAdapters;
        }

        public static ClusterCapabilities GetClusterCapabilities(this BucketConfig config)
        {
            return new ClusterCapabilities
            {
                Capabilities = config.ClusterCapabilities,
                Version = config.ClusterCapabilitiesVersion
            };
        }

        /// <summary>
        /// Replaces the $HOST placeholder with the Uri.Host used to bootstrap. This occurs in a single-node cluster.
        /// </summary>
        /// <param name="config">Configuration to update.</param>
        /// <param name="host">New host value for $HOST.</param>
        public static void ReplacePlaceholderWithBootstrapHost(this BucketConfig config, string host)
        {
            const string hostPlaceHolder = "$HOST";
            foreach (var nodesExt in config.NodesExt)
            {
                if (nodesExt.Hostname == null) nodesExt.Hostname = host;

                if (nodesExt.Hostname.Contains(hostPlaceHolder))
                    nodesExt.Hostname = nodesExt.Hostname.Replace(hostPlaceHolder, host);
            }

            foreach (var node in config.Nodes)
            {
                if (node.Hostname == null) node.Hostname = host;

                if (node.Hostname.Contains(hostPlaceHolder))
                    node.Hostname = node.Hostname.Replace(hostPlaceHolder, host);
            }

            if (config.VBucketServerMap?.ServerList != null)
            {
                var servers = config.VBucketServerMap.ServerList;
                for (var i = 0; i < servers.Length; i++)
                {
                    servers[i] = servers[i].Replace(hostPlaceHolder, host);
                }
            }
        }
    }
}
