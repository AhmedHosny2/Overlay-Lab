# Overlay Lab

![high system overview network view](https://github.com/user-attachments/assets/7cc186c5-feec-4022-bf1a-f049cead6004)

A platform for isolated programming exercises using **Docker** containers and a secure **VXLAN** overlay network.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Installation Guide](#installation-guide)
3. [Usage Instructions](#usage-instructions)
4. [Key Features](#key-features)
5. [Configuration](#configuration)
6. [Contributing Guidelines](#contributing-guidelines)
7. [Visual Enhancements](#visual-enhancements)
8. [License](#license)

---

## System Architecture Overview

This project provides an environment where students can complete programming exercises in isolation. Each exercise runs inside a dedicated Docker container and communicates over a secure VXLAN overlay network. The main objectives are:

- **Isolation:** Each exercise is **containerized**, ensuring a safe and independent testing environment.
- **Security:** A private **VXLAN overlay network keeps** container communications secure.
- **Flexibility:** Administrators can define exercises via JSON configuration, allowing for both client- and server-side challenges.

---

## Installation Guide

Follow these steps to set up your development environment and install necessary dependencies.

### Prerequisites

- [Docker](https://www.docker.com/get-started)
- [Multipass](https://multipass.run/)
- [NGINX](https://www.nginx.com/)

### Steps

1. **Clone the Repository:**

   ```bash
   git clone https://github.com/AhmedHosny2/testbed-distributed-system.git
   cd projectname

	2.	Install Dependencies:
Ensure Docker and Multipass are installed on your machine. For example, on Ubuntu:

sudo apt update && sudo apt install docker.io multipass nginx


	3.	Setup the Agent VM:
Launch the agent VM using Multipass:

multipass launch -n agent --cloud-init https://ovl.st.hs-ulm.de:4001/conf/user-data-mp.yaml jammy
multipass shell

Usage Instructions

After installation, follow these steps to start using the platform:

1. Register the VM in the Overlay Network

Inside the agent VM shell, register and start the overlay service:

# Register the VM
sudo vx register -o

# Start the overlay service
sudo vx start

# Test the connection
ping 10.1.0.2

2. Configure NGINX

Run the following commands to set up NGINX for mapping the server’s IP to server.local:

curl -o init_nginx.sh https://ulm.ahmed-yehia.me/init_nginx && chmod +x init_nginx.sh && sudo ./init_nginx.sh && \
curl -o update_srv_ip.sh https://ulm.ahmed-yehia.me/update_srv_ip && chmod +x update_srv_ip.sh && sudo ./update_srv_ip.sh

3. Finalize the Configuration

Run the configuration script to set up local settings:

curl -o agent_vm_ulm.ps1 https://ulm.ahmed-yehia.me/download && pwsh -ExecutionPolicy Bypass -File ./agent_vm_ulm.ps1

4. Access the Portal

Open your browser and navigate to:

http://server.local

Log in to view and select available exercises.

Key Features
	•	Container-Oriented Architecture:
Each exercise runs as a dedicated Docker container acting either as a client or server. This design isolates environments and simplifies management.
	•	Secure Overlay Network:
A VXLAN-based overlay ensures that container and VM communications remain private and conflict-free.
	•	NGINX Reverse Proxy:
Incoming traffic is managed by NGINX, which preserves user IPs, balances the load, and resolves DNS with friendly domain names.
	•	JSON-Defined Exercises:
Administrators can define exercises with customizable settings, including environment variables and network configurations.

Configuration

Exercises are defined via JSON files. Below is an example configuration:

Example JSON Configuration

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

	•	ExerciseName: Unique identifier.
	•	ExerciseReqConnectionType: Communication protocol (e.g., grpc).
	•	DockerImage: The container image to deploy.
	•	Variables: Key-value pairs for dynamic configuration.

Contributing Guidelines

We welcome contributions from the community! Please follow these steps:
	1.	Fork the Repository:
Click the Fork button on GitHub to create a copy of the repository.
	2.	Clone Your Fork:

git clone https://github.com/AhmedHosny2/testbed-distributed-system.git
cd projectname


	3.	Create a Feature Branch:

git checkout -b feature/your-feature-name


	4.	Make Your Changes:
Follow the existing coding style and add comments where necessary.
	5.	Run Tests:
Ensure your changes pass all tests.
	6.	Submit a Pull Request:
Describe your changes and the rationale behind them.

For more detailed guidelines, please see our CONTRIBUTING.md file.

Visual Enhancements

High System Overview

This diagram illustrates the overall architecture, highlighting Docker containers, the VXLAN overlay network, and the NGINX reverse proxy setup.

Example Screenshots
	•	Home Page Portal:
    ![home_page](https://github.com/user-attachments/assets/c7b611f9-feb5-4502-af76-ad892252c04d)


•	Start the Exercise: From the portal, choose a client-side exercise. Note the server container’s IP, port, or other details.
 ![ss 2025-02-02 at 12 19 39 AM](https://github.com/user-attachments/assets/dd73e656-1d3e-4f82-a620-9db1c79d1cf6)


•	View Container Details: Check the container’s status and networking information.
![ctonainer details ](https://github.com/user-attachments/assets/d1e159b2-b11b-47e8-8c44-f2b0bf3b16d0)
	•	Student Code & Response: Write your client code to send requests and observe the container’s response.
 
![user's code and resposne ](https://github.com/user-attachments/assets/ede43f22-2337-4cc6-993e-78f2c23700fd)



Solve Server-Side Exercises
Start the Exercise: From the portal, select a server-side exercise. You will be provided with an IP and port to listen on.
![ss 2025-01-26 at 11 58 18 PM](https://github.com/user-attachments/assets/3f5ff2ab-84c4-4313-871b-02d6aa0ea10d)


View Container Details: Check the container’s status (acting as the client) that sends test requests. Docker Container Details
![docker container details for user](https://github.com/user-attachments/assets/47082e9b-bf14-4d48-9c3a-80904ee3eac3)

Student Code & Response: Write your server code to handle incoming requests and verify the response. Student's Server Code
![student's server code](https://github.com/user-attachments/assets/beb774b1-2f21-479c-b5ce-2af3d84937a0)

