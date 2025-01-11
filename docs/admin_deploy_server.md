# Admin Guide: Deploying the Server for Exercises

This guide helps administrators set up a server VM from scratch, deploy the necessary code, and configure it for exercises. Follow the steps below to spin up and configure the server VM and deploy exercises.

---

## 1. Launch and Configure the Server VM

### Step 1.1: Create a VM Using Multipass
Run the following commands to create and configure a VM in the overlay network:

```bash
# Create a VM in the overlay network
multipass launch -n srv --cloud-init https://ovl.st.hs-ulm.de:4001/conf/user-data-mp.yaml jammy
```

### Step 1.2: Adjust Firewall Rules
Configure `iptables` to allow traffic within the overlay network:

```bash
sudo iptables -A INPUT -i vx -s 10.1.0.0/16 -j ACCEPT
sudo iptables -A INPUT -i vx -d 10.1.0.0/16 -j ACCEPT
```

---

## 2. Install Required Tools

### Step 2.1: Install Docker
Install Docker and configure it for the current user:

```bash
# Update and install Docker
sudo apt-get update
sudo apt-get upgrade -y
sudo apt-get install -y \
    ca-certificates \
    curl \
    gnupg \
    lsb-release
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Add Docker to the current user group
sudo groupadd docker
sudo usermod -aG docker $USER
newgrp docker
```

### Step 2.2: Install .NET SDK and Git
Install the required .NET SDK version and Git:

```bash
sudo apt update
sudo add-apt-repository ppa:dotnet/backports
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
sudo apt-get install -y git
```

---

## 3. Deploy and Test the Server

### Step 3.1: Run a Test Container
Run a simple Docker container to test that everything is working:

```bash
# Run the Docker container
docker run -p 5005:5005 ahmedyh1/time_app:arm64

# Test the running container
curl http://127.0.0.1:5005
```

You should see the current time returned by the container.

### Step 3.2: Register the VM in the Overlay Network
Register the server VM into the overlay network and start the overlay service:

```bash
sudo vx register -o
sudo vx start
```

---

## 4. Deploy the Exercise Portal

### Step 4.1: Clone the Repository
Clone the project repository and navigate to the portal directory:

```bash
# Clone the repository
git clone https://github.com/AhmedHosny2/testbed-distributed-system.git

# Navigate to the Portal directory
cd testbed-distributed-system/Portal/
```

### Step 4.2: Add Exercise Configurations
Create a configuration folder for exercises and add JSON files for each exercise:

```bash
mkdir ExConfiguration
cd ExConfiguration

# Create a configuration for the "Get-Time" exercise
nano Get-Time.json
```

#### Example `Get-Time.json`:
```json
{
  "ExerciseName": "Get-Time",
  "ExerciseReqConnectionType": "REST",
  "ExerciseTile": "Simple HTTP Get Request simulation",
  "ExerciseDescription": "This exercise will help you to understand how to send a GET request and receive a response from a server including the current time.",
  "ExerciseDifficulty": "1",
  "DockerImage": "ahmedyh1/time_app",
  "port": "5005",
  "DisplayFields": [
    "Name",
    "State.Status",
    "Config.Hostname",
    "State.*"
  ]
}
```

Add additional exercises, such as the following:

#### Example `socket-app.json`:
```json
{
  "ExerciseName": "socket-app",
  "ExerciseReqConnectionType": "socket",
  "ExerciseTile": "Simple Socket Connection",
  "ExerciseDescription": "This exercise will help you to understand how a socket connection works.",
  "ExerciseDifficulty": "1",
  "DockerImage": "ahmedyh1/socket_app",
  "port": "5007",
  "DisplayFields": [
    "Name",
    "State.Status",
    "Config.Hostname",
    "NetworkSettings.Networks.bridge.NetworkID"
  ]
}
```

### Step 4.3: Run the Portal
Start the exercise portal:

```bash
cd ../
dotnet run
```

The portal should now be accessible.

---

## 5. Configure the Server for Dynamic Firewalls

Dynamic firewalls may cause connectivity issues. Use a simple ping loop to keep the server alive:

### Step 5.1: Create the Ping Script
Create a script to continuously ping a switch in the overlay network:

```bash
nano ping_loop.sh
```

#### Content of `ping_loop.sh`:
```bash
#!/bin/bash

# Infinite loop to ping the IP every second
while true; do
    ping -c 1 10.1.0.2 >> ping_results.log 2>&1
    sleep 1
done
```

Make the script executable and run it in the background:

```bash
chmod +x ping_loop.sh
nohup ./ping_loop.sh &
```

### Step 5.2: Monitor and Stop the Script
To check the logs:
```bash
tail -f ping_results.log
```

To stop the script:
```bash
ps aux | grep ping_loop.sh
kill <PID>
```

---

## 6. Testing the Server
Once the server is live and running:
- Follow the [Student Guide](https://github.com/AhmedHosny2/testbed-distributed-system/blob/main/docs/student_solve_exercise.md) to test and solve exercises.

---

Your server is now ready for use with both client-side and server-side exercises.