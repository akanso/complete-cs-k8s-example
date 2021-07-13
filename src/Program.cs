using System;
using System.Threading;
using Microsoft.Rest;
using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.Threading.Tasks;



namespace complete
{
    using CreateLambda = Func<IKubernetes, IKubernetesObject, string, CancellationToken, Task<IKubernetesObject>>;
    using DeleteLambda = Func<IKubernetes, string, string, CancellationToken, Task<IKubernetesObject>>;

   
    public class Program
    {
        /// <summary>
        /// CreateMethods is a dictionary of methods, used to find the appropriate create method based on the object Kind
        /// </summary>
        private static readonly Dictionary<string, CreateLambda> CreateMethods = new Dictionary<string, CreateLambda>()
        {
            [V1ConfigMap.KubeKind] = async (k, b, ns, ct) => await k.CreateNamespacedConfigMapAsync((V1ConfigMap)b, ns, cancellationToken: ct),
            [V1Secret.KubeKind] = async (k, b, ns, ct) => await k.CreateNamespacedSecretAsync((V1Secret)b, ns, cancellationToken: ct),
            [V1Deployment.KubeKind] = async (k, b, ns, ct) => await k.CreateNamespacedDeploymentAsync((V1Deployment)b, ns, cancellationToken: ct),
            [V1Service.KubeKind] = async (k, b, ns, ct) => await k.CreateNamespacedServiceAsync((V1Service)b, ns, cancellationToken: ct),
            [V1Pod.KubeKind] = async (k, n, ns, ct) => await k.CreateNamespacedPodAsync((V1Pod)n, ns, cancellationToken: ct)
        };

        /// <summary>
        /// DeleteMethods is a dictionary of methods, used to find the appropriate delete method based on the object Kind
        /// </summary>
        private static readonly Dictionary<string, DeleteLambda> DeleteMethods = new Dictionary<string, DeleteLambda>()
        {
            [V1ConfigMap.KubeKind] = async (k, n, ns, ct) => await k.DeleteNamespacedConfigMapAsync(n, ns, cancellationToken: ct),
            [V1Secret.KubeKind] = async (k, n, ns, ct) => await k.DeleteNamespacedSecretAsync(n, ns, cancellationToken: ct),
            [V1Deployment.KubeKind] = async (k, n, ns, ct) => await k.DeleteNamespacedDeploymentAsync(n, ns, cancellationToken: ct),
            [V1Service.KubeKind] = async (k, n, ns, ct) => await k.DeleteNamespacedServiceAsync(n, ns, cancellationToken: ct),
            [V1Pod.KubeKind] = async (k, n, ns, ct) => await k.DeleteNamespacedPodAsync(n, ns, cancellationToken: ct)
        };

        public static Kubernetes client;
        public static CancellationToken cancellationToken;

        // if the env var is not set, we use the default k8s namespace for KubeNamespace
        private static string KubeNamespace => !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("NAMESPACE")) ? Environment.GetEnvironmentVariable("NAMESPACE") : "default";
        private async static Task Main(string[] args)
        {
            Console.WriteLine($"Program started... in namespace = {KubeNamespace}");
            
            // Initializes a new instance of the k8s.KubernetesClientConfiguration from default
            // locations If the KUBECONFIG environment variable is set, then that will be used.
            // Next, it looks for a config file at k8s.KubernetesClientConfiguration.KubeConfigDefaultLocation.
            // Then, it checks whether it is executing inside a cluster and will use k8s.KubernetesClientConfiguration.InClusterConfig.
            // Finally, if nothing else exists, it creates a default config with localhost:8080
            // as host.
            KubernetesClientConfiguration config = KubernetesClientConfiguration.BuildDefaultConfig();

            // cancellationToken used with async() calls
            CancellationTokenSource source = new CancellationTokenSource();
            cancellationToken = source.Token;

            // client is a k8s client used to issue the HTTP calls to K8s
            client = new Kubernetes(config);

            // objects a list used to store the objects
            List<IKubernetesObject> objects = new List<IKubernetesObject>();


            /// create the resources definitions:
            /// the order is important here. The secret and configmaps will be used
            /// when deploying the deployment
            
            // create secret
            IKubernetesObject mySecret = Factory.CreateSecretDefinition();
            objects.Add(mySecret);
            /// create configmaps
            IKubernetesObject myConfigMap = Factory.CreateConfigMapDefinition();
            objects.Add(myConfigMap);
            // create configmap to be mounted as volume
            IKubernetesObject myMountedConfigMap = Factory.CreateMountConfigMapDefinition();
            objects.Add(myMountedConfigMap);
            // create deployment
            IKubernetesObject myDeployment = Factory.CreateDeploymentDefinition();
            objects.Add(myDeployment);
            // create service
            IKubernetesObject myService = Factory.CreateServiceDefinition();
            objects.Add(myService);
            // create pod
            IKubernetesObject myPod = Factory.CreatePodDefinition();
            objects.Add(myPod);

            // delete all existing replicas of our objects
            await Cleanup(objects);

            // giving K8s some time to delete the entities
            Thread.Sleep(5000);

            // deploy all objects to K8s
            await DeployAll(objects);

        }

        /// <summary>
        /// Cleanup deletes all the objects
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public async static Task Cleanup(List<IKubernetesObject> objects)
        {
            foreach (IKubernetesObject item in objects)
            {
                await DeleteEntities(item);
            }
        }

        /// <summary>
        /// DeployAll deploy all the objects
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public async static Task DeployAll(List<IKubernetesObject> objects)
        {
            foreach (IKubernetesObject item in objects)
            {
                await DeployEntities(item);
            }
        }

        /// <summary>
        ///DeployEntities takes a K8s object -> fetches the corresponding create function -> deploys the object
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public static async Task DeployEntities(IKubernetesObject definition)
        {
            try
            {
                CreateLambda create = CreateMethods[definition.Kind];
                await create(client, definition, KubeNamespace, cancellationToken);
                Console.WriteLine($"{definition.Kind} created");
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                Console.Error.WriteLine($"{definition.Kind} already exists");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"An error occured while trying to create resource: {e}");
            }
        }
        /// <summary>
        /// DeleteEntities takes a K8s object -> fetches the corresponding create function -> deletes the object
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public async static Task DeleteEntities(IKubernetesObject definition)
        {  
            try
            {
                DeleteLambda delete = DeleteMethods[definition.Kind];
                await delete(client, GetName(definition), KubeNamespace, cancellationToken);
                Console.WriteLine($"{definition.Kind} {GetName(definition)} deleted");
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.Error.WriteLine($"{definition.Kind} already deleted");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"An error occured while trying to delete {definition.Kind} resource: {e}");
            }
        }

        /// <summary>
        /// GetName used to get a Name from a generic IKubernetesObject
        /// </summary>
        /// <param name="kubernetesObject"></param>
        /// <returns></returns>/
        public static string GetName(IKubernetesObject kubernetesObject)
        {
            return kubernetesObject.Kind switch
            {
                V1ConfigMap.KubeKind => ((V1ConfigMap)kubernetesObject).Metadata.Name,
                V1Secret.KubeKind => ((V1Secret)kubernetesObject).Metadata.Name,
                V1Deployment.KubeKind => ((V1Deployment)kubernetesObject).Metadata.Name,
                V1Service.KubeKind => ((V1Service)kubernetesObject).Metadata.Name,
                V1Pod.KubeKind => ((V1Pod)kubernetesObject).Metadata.Name,
                _ => throw new InvalidCastException($"Cannot cast {kubernetesObject.Kind} to a k8s class"),
            };
        }
    }
}
