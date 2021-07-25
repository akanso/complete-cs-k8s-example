
namespace complete
{
    using k8s.Models;
    using System.Collections.Generic;
    using System.Text;
    public static class Factory
    {
        static readonly string  SharedMemorySizeLimit = "100Mi";

        // create deployment
        public static V1Deployment CreateDeploymentDefinition()
        {
            V1Deployment deploy = new V1Deployment()
            {
                ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                Kind = V1Deployment.KubeKind,
                Metadata = new V1ObjectMeta()
                {
                    Name = "my-deployment",
                    Annotations = new Dictionary<string, string>
                    {
                        ["key1"] = "value1",
                        ["key2"] = "value2",
                    },
                    Labels = new Dictionary<string, string>()
                    {
                        ["key1"] = "value1",
                        ["key2"] = "value2",
                    }
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = 2,
                    Selector = new V1LabelSelector()
                    {
                        MatchLabels = new Dictionary<string, string>()
                        {
                            ["app"] = "myapplication",
                        }
                    },
                    Template = new V1PodTemplateSpec()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            CreationTimestamp = null,
                            Labels = new Dictionary<string, string>()
                            {
                                ["app"] = "myapplication",
                                ["key1"] = "value1",
                                ["key2"] = "value2",
                            },
                        },
                        Spec = new V1PodSpec
                        {
                            EnableServiceLinks = false,
                            Volumes = new List<V1Volume>
                            {
                                new V1Volume
                                {
                                    Name = "shm-volume",
                                    EmptyDir = new V1EmptyDirVolumeSource
                                    {
                                        Medium = "Memory",
                                        SizeLimit = !string.IsNullOrWhiteSpace(SharedMemorySizeLimit)?
                                        new ResourceQuantity($"{SharedMemorySizeLimit}"): null,
                                    },
                                },
                                new V1Volume
                                {
                                    Name = "host-volume",
                                    HostPath = new V1HostPathVolumeSource
                                    {
                                        Path = "/home/",
                                        Type = "DirectoryOrCreate",
                                    },
                                },
                                // my-mounted-config
                                new V1Volume
                                {
                                    Name = "configmap-volume",
                                    ConfigMap = new V1ConfigMapVolumeSource
                                    {
                                        Name = "my-mounted-config",
                                        Items = new List<V1KeyToPath>
                                        {
                                            new V1KeyToPath
                                            {
                                                Key = "html",
                                                Path = "index.html"
                                            },
                                        },
                                    },
                                },
                            },
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Name = "my-container",
                                    Image = "nginx",
                                    ImagePullPolicy = "IfNotPresent",
                                    Env = new[]
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "ENV1",
                                            Value = "regular-env1",
                                        },
                                        new V1EnvVar
                                        {
                                            Name = "ENV2",
                                            Value = "regular-env2",
                                        },
                                        new V1EnvVar
                                        {
                                            // env variables using K8s secret specific key
                                            Name = "ENVFROMSECRET",
                                            ValueFrom = new V1EnvVarSource{
                                                SecretKeyRef = new V1SecretKeySelector
                                                {
                                                    Name = "my-secret",
                                                    Key = "SECRET_KEY",
                                                },
                                            },
                                        },
                                        new V1EnvVar
                                        {
                                            // env variables using K8s Downward API
                                            Name = "MY_POD_IP",
                                            ValueFrom = new V1EnvVarSource
                                            {
                                                FieldRef = new V1ObjectFieldSelector
                                                {
                                                    FieldPath = "status.podIP"
                                                }
                                            }
                                        },
                                        new V1EnvVar
                                        {
                                            // env variables using K8s Downward API
                                            Name = "MY_NAMESPACE",
                                            ValueFrom = new V1EnvVarSource
                                            {
                                                FieldRef = new V1ObjectFieldSelector
                                                {
                                                    FieldPath = "metadata.namespace"
                                                }
                                            }
                                        },
                                    },
                                    EnvFrom = new []
                                    {
                                        // env variables from ConfigMap
                                        new V1EnvFromSource
                                        {
                                            ConfigMapRef = new V1ConfigMapEnvSource
                                            {
                                                Name = $"my-config",
                                            }
                                        },
                                        // env variables from secret
                                        new V1EnvFromSource
                                        {
                                            SecretRef = new V1SecretEnvSource
                                                {
                                                    Name = $"my-secret",
                                                }
                                        }
                                    },
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = 80
                                        },
                                    },
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new V1VolumeMount
                                        {
                                            Name = "shm-volume",
                                            MountPath = "/dev/shm",
                                        },
                                         new V1VolumeMount
                                        {
                                            Name = "configmap-volume",
                                            MountPath = "/usr/share/nginx/html",
                                        },
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Limits = new Dictionary<string, ResourceQuantity>
                                        {
                                           ["memory"] = new ResourceQuantity("200Mi"),
                                           ["cpu"] = new ResourceQuantity("100m"),
                                        },
                                        Requests = new Dictionary<string, ResourceQuantity>
                                        {
                                           ["memory"] = new ResourceQuantity("100Mi"),
                                           ["cpu"] = new ResourceQuantity("50m"),
                                        },
                                    },
                                    LivenessProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Port = 80,
                                        }
                                    }
                                },
                            },
                        },
                    },
                },
            };
            return deploy;
        }

        // create configmap
        public static V1ConfigMap CreateConfigMapDefinition()
        {
            V1ConfigMap config = new V1ConfigMap()
            {
                ApiVersion = $"{V1ConfigMap.KubeGroup}/{V1ConfigMap.KubeApiVersion}",
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta()
                {
                    Name = "my-config",
                    Annotations = new Dictionary<string, string>
                    {
                        ["key1"] = "value1",
                        ["key2"] = "value2",
                    },
                    Labels = new Dictionary<string, string>()
                    {
                        ["key1"] = "value1",
                        ["app"] = "myapplication",
                    },
                },
                Data = new Dictionary<string, string>()
                {
                    ["key1"] = "data1-from-configmap",
                    ["key2"] = "data2-from-configmap",
                },
            };
            return config;
        }
        /// <summary>
        /// This is a config map to be mounted as a volume
        /// </summary>
        /// <returns></returns>
        public static V1ConfigMap CreateMountConfigMapDefinition()
        {
            V1ConfigMap config = new V1ConfigMap()
            {
                ApiVersion = $"{V1ConfigMap.KubeGroup}/{V1ConfigMap.KubeApiVersion}",
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta()
                {
                    Name = "my-mounted-config",
                    Annotations = new Dictionary<string, string>
                    {
                        ["key1"] = "value1",
                        ["key2"] = "value2",
                    },
                    Labels = new Dictionary<string, string>()
                    {
                        ["key1"] = "value1",
                        ["app"] = "myapplication",
                    },
                },
                Data = new Dictionary<string, string>()
                {
                    ["html"] = "<!DOCTYPE html><html><body><h1 style=background-color:DodgerBlue;>Hello C-Sharp!</h1></body></html>",
                },
            };
            return config;
        }

        // create secret
        public static V1Secret CreateSecretDefinition()
        {
            V1Secret secret = new V1Secret()
            {
                ApiVersion = $"{V1Secret.KubeGroup}/{V1Secret.KubeApiVersion}",
                Kind = V1Secret.KubeKind,
                Metadata = new V1ObjectMeta()
                {
                    Name = "my-secret",
                    Annotations = new Dictionary<string, string>
                    {
                        ["key1"] = "value1",
                        ["key2"] = "value2",
                    },
                    Labels = new Dictionary<string, string>()
                    {
                        ["key1"] = "value1",
                        ["app"] = "myapplication",
                    },
                },
                Data = new Dictionary<string, byte[]>()
                {
                    ["SECRET_KEY"] = Encoding.Default.GetBytes("data-from-secret"),
                    ["SECRET_KEY_TWO"] = Encoding.Default.GetBytes("more-data-from-secret"),
                },
            };
            return secret;
        }

        // create service
        public static V1Service CreateServiceDefinition()
        {

            V1Service service = new V1Service()
            {
                ApiVersion = $"{V1Service.KubeGroup}/{V1Service.KubeApiVersion}",
                Kind = V1Service.KubeKind,
                Metadata = new V1ObjectMeta()
                {
                    Name = "my-service",
                    Annotations = new Dictionary<string, string>
                    {
                        ["key1"] = "value1",
                        ["key2"] = "value2",
                    },
                    Labels = new Dictionary<string, string>()
                    {
                        ["key1"] = "value1",
                        ["app"] = "myapplication",
                    },
                },
                Spec = new V1ServiceSpec
                {
                    Type = "NodePort",
                    Selector = new Dictionary<string, string>
                    {
                        ["app"] = "myapplication",
                    },
                    Ports = new List<V1ServicePort>{
                        new V1ServicePort{
                            Protocol = "TCP",
                            Port = 80,
                            TargetPort = 80,
                            NodePort = 30001, //default range: 30000-32767
                        },
                    }
                }
            };
            return service;
        }
        /// <summary>
        /// CreatePodDefinition creates a pod with:
        /// - init container that waits for the service to available in the DNS server
        /// - readliness probes 
        /// </summary>
        /// <returns></returns>
        public static V1Pod CreatePodDefinition()
        {
            V1Pod pod = new V1Pod()
            {
                ApiVersion = $"{V1Pod.KubeGroup}/{V1Pod.KubeApiVersion}",
                Kind = V1Pod.KubeKind,
                Metadata = new V1ObjectMeta()
                {
                    Name = "my-pod",
                    Annotations = new Dictionary<string, string>
                    {
                        ["key1"] = "value1",
                        ["key2"] = "value2",
                    },
                    Labels = new Dictionary<string, string>()
                    {
                        ["key1"] = "value1",
                        ["app"] = "myotherapplication",
                    },
                },
                Spec = new V1PodSpec
                {
                    InitContainers = new List<V1Container>()
                    {
                        new V1Container()
                        {
                            Name = "init-myservice",
                            Image = "busybox:1.28",
                            ImagePullPolicy = "Always",
                            Command = new List<string>() { "sh", "-c", "until nslookup my-service.$(cat /var/run/secrets/kubernetes.io/serviceaccount/namespace).svc.cluster.local; do echo waiting for myservice; sleep 2; done" }
                        }
                    },
                    Containers = new List<V1Container>()
                    {
                        new V1Container()
                        {
                            Name = "my-container",
                            Image = "busybox",
                            ImagePullPolicy = "IfNotPresent",
                            Command =  new List<string>{"sh", "-c", "while true; do nc -z -v my-service 80; sleep 10; done"},
                            Env = new[]
                            {
                                new V1EnvVar
                                {
                                    Name = "ENV1",
                                    Value = "regular-env1",
                                },
                            },
                            Resources = new V1ResourceRequirements
                            {
                                Limits = new Dictionary<string, ResourceQuantity>
                                {
                                    ["memory"] = new ResourceQuantity("200Mi"),
                                    ["cpu"] = new ResourceQuantity("100m"),
                                },
                                Requests = new Dictionary<string, ResourceQuantity>
                                {
                                    ["memory"] = new ResourceQuantity("100Mi"),
                                    ["cpu"] = new ResourceQuantity("50m"),
                                },
                            },
                            ReadinessProbe = new V1Probe
                            {
                                Exec = new V1ExecAction
                                {
                                    Command = new List<string>
                                    {
                                        "sh", "-c", " nc -zv my-service 80",
                                    },
                                }
                            }
                        },
                    },
                }
            };
            return pod;
        }
    }
}
