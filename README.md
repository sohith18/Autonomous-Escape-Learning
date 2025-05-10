# Unity ML-Agents Project: Escape Agent

This project implements a Unity ML-Agents environment where an agent learns to navigate and escape a room using reinforcement learning. The agent observes the environment using raycasts and vector data and is trained using PPO, SAC, or a custom DQN implementation.

---

## Included in this Zip

- Unity project folder
- All the config files (in `Assets/config`)
- Trained model `.onnx` files (inside `Assets/config/results.`. Each folder contains a trained model `.onnx` file)
- Sample scene
- Scripts and raycasting setup
- `README.md` (this file)

---

## Requirements

- Unity (recommended: 2023.2.20f1)
- Python 3.8–3.10
- ML-Agents Toolkit v0.30.0

Install Ml-Agents extension in Unity if not already installed.

Here is a link which shows how to setup ML-Agents Python environment. In the tutorial, it asks to create a new config folder. But our submitted project folder already contains it. So, just navigate to that folder and follow the rest of the instructions.
[text](https://docs.google.com/presentation/d/1ubDFDqHhjEau24w7vM9gFdhhpwKEVEM6KYEkei1ha-Q/edit#slide=id.g2e6deb9682a_0_1863)

---

## Training Instructions

### 1. Open the Project
- Extract the zip.
- Open Unity Hub → "Open Project" → select this folder.
- Select the `SampleScene` scene in the `Assets/Scenes` folder.

### 2. Configure the Agent
- Select the agent GameObject in the scene.
- Set **Behavior Name** to match the YAML file (e.g., `EscapeAgent`).
- Set **Behavior Type** to `Default`.

### 3. Train with ML-Agents
In terminal:

```bash
mlagents-learn <config_file_name>.yaml --run-id=escape_run --force
```

If you're using a build instead of the Unity Editor:

```bash
mlagents-learn <config_file_name>.yaml --run-id=escape_run --env=Builds/RLEnvironment.app --force
```
---

## Inference Instructions

### 1. Assign the Trained Model
- After training, assign the `.onnx` model to the agent under `Behavior Parameters → Model`.

### 2. Set to Inference Mode
- Set **Behavior Type** to `Inference Only`.

### 3. Run in Unity
- Press **Play** to test the trained agent.

## Notes
- If you are trying to run inference for models which were trained on discrete action space, you need to change actions in `Behavior Parameters → Actions`. Set `Continuous Actions` to 0 and `Discrete Branches` to 
- Then set `Branch 0 size` to 5 and `Branch 1 size` to 2.
- For running inference on models with stacked vectors, change `Behavior Parameters → Vector Observation → Stacked Vector` to 5.
- Then you need to change the actions in `Assets/EscapeAgent.cs`. Uncomment the commented code at the bottom of the file and comment out the top part.
- For running inference on models which were trained on an environment without `VerticalRotator`, removing it would give the best results. To remove it, click on the `VerticalRotator` in the scene and uncheck it in the inspector on the right.


---

## Visualize Training (Optional)

Use TensorBoard:
```bash
tensorboard --logdir results
```
Then visit: http://localhost:6006

---

## Notes

- Training logs are saved to the `Assets/config/results/` directory.
- Training can take several hours depending on environment complexity.
- For training DQN, you need to first build the Unity environment with discrete action space, then change the path to the build in `Assets/config/DQN.py` file. Before building, you need to change the actions in `Assets/EscapeAgent.cs`. Uncomment the commented code at the bottom of the file and comment out the top part. Then you can just run the Python file for training.


---
