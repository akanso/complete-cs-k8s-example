
# Image URL to use all building/pushing image targets

IMG ?=kube-complete-example:1.0

# Run against the configured Kubernetes cluster in ~/.kube/config
run:
	dotnet run

# Build the docker image
docker-build:
	docker build . -t ${IMG}

# Push the docker image
docker-push:
	docker push ${IMG}


# deploy to k8s
deploy:
	kubectl apply -f config/

# clean up k8s created objects
cleanup:
	kubectl delete -f config/
