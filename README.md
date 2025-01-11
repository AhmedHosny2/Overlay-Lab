# Educational Programming Platform
![high system overview](https://github.com/user-attachments/assets/1db2cfcd-a116-474a-a32d-4e3ecc6f6750)


This platform simplifies software development exercises by automating containerized environments and managing network configurations. Designed for educational use, it supports hands-on learning for students and streamlined administration for instructors.

---

## Key Features

- **Flexible Exercise Management**: Add exercises using modular JSON files.
- **Dynamic Container Orchestration**: Automates lifecycle, IP, and port management for exercises.
- **Two Exercise Types**:
  - **Server-Side**: Students implement servers to handle simulated client requests.
  - **Client-Side**: Students write clients to interact with provided servers.
- **Secure Networking**: Uses a VXLAN overlay network to isolate and protect communication.
- **Real-Time Monitoring**: Tools for tracking active containers and usage.

---


## How It Works

1. **Platform Deployment**: 
   - The platform itself is hosted on a dedicated VM within the VXLAN overlay network.
   - The overlay network ensures private, secure communication.

2. **Exercise Containers**:
   - Docker containers are dynamically launched for each exercise.
   - Containers can simulate a server (for client-side exercises) or a client (for server-side exercises).

3. **Student Interaction**:
   - Students log in via the web interface and select an exercise.
   - They receive container details (e.g., IP, port) and write client or server code to interact with the exercise.
  ### Deployment Workflow

![Deployment Diagram Page 2](https://github.com/user-attachments/assets/63523871-6098-4310-a9b1-568cb1898f56)

---
## Visual Walkthrough

![Sequence Diagram Create Server](https://github.com/user-attachments/assets/d5050e6e-b2de-4664-a415-91a952a6ac8c)
![home_page](https://github.com/user-attachments/assets/a1e3a7e6-8297-4860-a6d4-c231eb26368e)


![server_details](https://github.com/user-attachments/assets/620c81b4-a92d-4027-b31d-6c16cbca0429)


## Example JSON Configuration

```json
{
  "ExerciseName": "Get-Time",
  "ExerciseReqConnectionType": "REST",
  "ExerciseTile": "HTTP Get Request Simulation",
  "ExerciseDescription": "Learn to send GET requests and handle responses.",
  "DockerImage": "ahmedyh1/time_app",
  "port": "5005"
}
```

---

## Technical Highlights

- **Platform Hosting**:
  - Hosted on a VM within a VXLAN overlay network for secure communication.
  - NGINX reverse proxy ensures accurate client-server interactions.

- **Exercise Containers**:
  - Docker containers are spun up dynamically based on JSON exercise configurations.
  - Containers communicate securely over the overlay network.

- **Authentication**:
  - Azure Active Directory integration for secure user login.

- **Supports Multiple Programming Languages**:
  - Python, Java, TypeScript, C#, and Go.

---


## For More Details

- **[Admin: How to Deploy the Server](https://github.com/AhmedHosny2/testbed-distributed-system/blob/main/docs/admin_deploy_server.md)**: Step-by-step instructions to set up the server VM and deploy the platform.
- **[Admin: How to Create an Exercise](https://github.com/AhmedHosny2/testbed-distributed-system/blob/main/docs/admin_create_exercise.md)**: Guide for defining and configuring exercises.
- **[Student: How to Solve an Exercise](https://github.com/AhmedHosny2/testbed-distributed-system/blob/main/docs/student_solve_exercise.md)**: Instructions for students to interact with and solve exercises.


