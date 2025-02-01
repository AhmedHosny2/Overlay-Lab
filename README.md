![high system overview](https://github.com/user-attachments/assets/3e16e1e8-c002-4311-b371-5f6d70cd7c2a)
# High System Overview

This platform provides isolated programming exercises using **Docker** containers and a secure **VXLAN** overlay network. Students focus on writing code (client or server), while each exercise runs as a container in a private environment. Follow the steps below to set up a gateway VM, connect to the overlay network, and interact with the exercises.



## Container-Oriented

Each exercise runs as a Docker container. Depending on the exercise, this container acts as either a **client** or a **server**. Students only write the complementary code.

## Secure Overlay

We employ a VXLAN-based overlay network to keep communications private and prevent conflicts on shared infrastructure. Containers and student VMs connect through this overlay without exposing ports publicly.

## NGINX Reverse Proxy

Incoming traffic is routed through **NGINX**, which preserves user IPs and balances load. It also simplifies DNS resolution, allowing connections via a user-friendly domain (e.g., `server.local`).

## JSON-Defined Exercises

Administrators define exercises with a simple JSON file. Each configuration can include environment variables, maximum user limits (`maxUsers`), and more. The container uses these definitions to set up networking, ports, and any custom environment variables.

### Example JSON Configuration

```json
{
  "ExerciseName": "grpc-app",
  "ExerciseReqConnectionType": "grpc",
  "ExerciseTile": "gRPC Adventure",
  "ExerciseDescription": "Dive into the world of gRPC! Build a client that connects to a gRPC server and interacts with it to create, fetch, list, and delete users.",
  "ExerciseDifficulty": "2",
  "DockerImage": "ahmedyh1/grcp_server",
  "port": "5015",
  "DisplayFields": [
    "Name",
    "State.Status",
    "Config.Hostname",
    "NetworkSettings.Networks.bridge.NetworkID"
  ],
  "ClientSide": false,
  "Variables": {
    "host": "",
    "exposedPort": ""
  }
}
```

	•	ExerciseName: Unique identifier (“grpc-app”).
	•	ExerciseReqConnectionType: Communication type (“grpc”).
	•	DockerImage: Container image to launch.
	•	Variables: Environment values (e.g., host, port mappings).

Solving Exercises as a Student

This guide assumes basic familiarity with overlay networks. For further context, see section 1.4 of this guide.

Step 1: Set Up the Agent VM
	1.	Install Multipass
Ensure Multipass is installed on your machine.
	2.	Launch the Agent VM

``multipass launch -n agent --cloud-init https://ovl.st.hs-ulm.de:4001/conf/user-data-mp.yaml jammy``
``multipass shell``

	•	Once created, you’ll automatically enter the VM shell.

Step 2: Register the VM in the Overlay Network

Inside the VM shell, execute:

# Register the VM
``sudo vx register -o``

# Start the overlay service
``sudo vx start``

# Test the connection
``ping 10.1.0.2``

If you see replies, your VM is connected to the overlay.

Now configure NGINX and map the server’s IP to server.local:

``curl -o init_nginx.sh https://ulm.ahmed-yehia.me/init_nginx && chmod +x init_nginx.sh && sudo ./init_nginx.sh && \
curl -o update_srv_ip.sh https://ulm.ahmed-yehia.me/update_srv_ip && chmod +x update_srv_ip.sh && sudo ./update_srv_ip.sh``

Step 3: Run the Configuration Script

Finalize local machine settings within the VM:

``curl -o agent_vm_ulm.ps1 https://ulm.ahmed-yehia.me/download && pwsh -ExecutionPolicy Bypass -File ./agent_vm_ulm.ps1``

After running this, your agent VM and local environment are fully configured for the overlay network.

Step 4: Access and Solve Exercises
	1.	Access the Portal
Open a browser on your local machine and navigate to:

server.local

Log in to view available exercises.
 ![HOMe page](https://github.com/user-attachments/assets/6d417bfd-fc94-4e78-94f3-5c1457bb7ff2)


2.	Solve Client-Side Exercises
 ![grcp example detials ](https://github.com/user-attachments/assets/251a9ef1-ec5b-4dfe-a367-eac5530c4a86)

Start the Exercise:
From the portal, choose a client-side exercise. Note the server container’s IP, port, or other details.

View Container Details:
Check the container’s status and networking information.
![ex container details ](https://github.com/user-attachments/assets/bdfce7b8-c587-44db-8ec3-ce71b2fdd47e)

Student Code & Response:
Write your client code to send requests and observe the container’s response.
![user's things](https://github.com/user-attachments/assets/a14b3453-dc03-4d16-b42c-5e3f0e2de5ae)

3.	Solve Server-Side Exercises
 ![ss 2025-01-26 at 11 58 18 PM](https://github.com/user-attachments/assets/0cbbe410-0e78-4dc2-ae77-52d89b18f7ae)

Start the Exercise:
From the portal, select a server-side exercise. You will be provided with an IP and port to listen on.

View Container Details:
Check the container’s status (acting as the client) that sends test requests.
![docker container details for user](https://github.com/user-attachments/assets/50b46519-2d5c-4f25-9572-88cfa1b37fd4)

Student Code & Response:
Write your server code to handle incoming requests and verify the response.
![student's server code](https://github.com/user-attachments/assets/616fd1fe-9f16-49ea-a9ec-201d89ac8c7f)

