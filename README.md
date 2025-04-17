# RBF_Interpolation


Overview

This Unity v6000.0.23f1 project focuses on Radial Basis Function (RBF) interpolation for robotic arm motion control. We simulate a KUKA iiwa‑7 arm using Unity’s ArticulationBodies (powered by NVIDIA PhysX) repository given by my supervisor Prof.Eyal Ofek to develop on top of this and drive it via a custom ArticulationDriver that records expert‑defined pose–angle samples. At runtime, the RBF engine reads these sparse dataset entries, selects the nearest neighbors to any new target, and blends their joint configurations through inverse‑distance weighting and quaternion averaging. This lightweight, data‑driven approach generates smooth, real‑time joint trajectories without on‑the‑fly inverse‑kinematics solving or heavy dynamic modeling.



Getting Started

Clone the repository

git clone git@github.com:Arvin-Zi/H2B_MotionBridge.git
cd H2B_MotionBridge

Open in Unity

Launch Unity Hub, click Add, and select this project’s root folder.

Ensure Unity 6000.0.23f1 is installed, then click Open.

Dependencies

Install the Ultra package for Leap Motion hand‑tracking input.

Alternatively, use a 3D sphere in the scene as a target.



Developed by Seyed Arvin Ziaei, University of Birmingham (2024–25).