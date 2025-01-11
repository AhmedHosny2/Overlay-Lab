# Educational Programming Platform

![High System Overview](https://github.com/user-attachments/assets/1db2cfcd-a116-474a-a32d-4e3ecc6f6750)
*Figure: High-level system architecture overview*

This platform revolutionizes educational programming exercises by leveraging modern technologies like **Docker containers**, **Razor Pages**, **NGINX**, and a **VXLAN-based overlay network**. It simplifies exercise management and automates containerized environments, providing hands-on learning for students and efficient administration tools for instructors.

---

## Key Features

- **Modular Exercise Management**: Define and manage exercises using JSON configurations.
- **Dynamic Docker Container Orchestration**: Automates container lifecycle, IP, and port assignments for exercises.
- **Two Exercise Modes**:
  - **Server-Side Exercises**: Students develop servers to handle client requests simulated by the platform.
  - **Client-Side Exercises**: Students write clients to interact with provided server containers.
- **Secure Networking**: Ensures private, reliable communication using a VXLAN overlay network.
- **Real-Time Monitoring**: Track active containers, resource usage, and user activity.

---

## How It Works

1. **Platform Hosting**:
   - Hosted on a dedicated **VM** within a secure **VXLAN overlay network**.
   - The **NGINX reverse proxy** preserves user IPs and handles traffic.

2. **Exercise Containers**:
   - **Docker containers** dynamically simulate client or server roles for exercises.
   - Containers are isolated and communicate via the overlay network.

3. **Student Interaction**:
   - Students log in via a **Razor Pages-based** web interface.
   - Select exercises, receive container details, and write code (client or server) to interact with the containers.

### Deployment Workflow
![Deployment Diagram](https://github.com/user-attachments/assets/63523871-6098-4310-a9b1-568cb1898f56)
*Figure: Deployment process illustrating VM setup, container orchestration, and overlay network integration.*

---

## Visual Walkthrough

### 1. Home Page
![Home Page](https://github.com/user-attachments/assets/a1e3a7e6-8297-4860-a6d4-c231eb26368e)
*Browse available exercises and their configurations.*

### 2. Server Details
![Server Details](https://github.com/user-attachments/assets/620c81b4-a92d-4027-b31d-6c16cbca0429)
*View real-time details of your Docker container for active exercises.*

### 3. Sequence Diagram for Exercise Creation
![Sequence Diagram](https://github.com/user-attachments/assets/d5050e6e-b2de-4664-a415-91a952a6ac8c)
*How exercises are created and managed through the system.*

---

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
  - VM hosted within a **VXLAN overlay network** for secure communication.
  - **NGINX reverse proxy** ensures traffic routing and preserves user IPs.

- **Exercise Containers**:
  - **Docker** orchestrates lightweight containers for exercises.
  - Containers interact securely over the overlay network.

- **Authentication**:
  - **Azure Active Directory** for secure user authentication and access control.

- **Programming Language Support**:
  - Exercises support Python, Java, TypeScript, C#, and Go.

---

## For More Details

- **[Admin: How to Deploy the Server](https://github.com/AhmedHosny2/testbed-distributed-system/blob/main/docs/admin_deploy_server.md)**: Step-by-step instructions to set up the server VM and deploy the platform.
- **[Admin: How to Create an Exercise](https://github.com/AhmedHosny2/testbed-distributed-system/blob/main/docs/admin_create_exercise.md)**: Guide for defining and configuring exercises.
- **[Student: How to Solve an Exercise](https://github.com/AhmedHosny2/testbed-distributed-system/blob/main/docs/student_solve_exercise.md)**: Instructions for students to interact with and solve exercises.

---

This project demonstrates expertise in **containerization**, **web development**, and **networking**, providing a secure, scalable, and user-friendly environment for educational programming.
