import random
import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from collections import deque

# Define DQN network
class DQN(nn.Module):
    def __init__(self, ray_obs_size, vec_obs_size, n_actions):
        super(DQN, self).__init__()
        
        # Ray Perception Encoder (using simple Linear layers)
        # self.ray_encoder = nn.Sequential(
        #     nn.Linear(ray_obs_size, 128),
        #     nn.ReLU(),
        #     nn.Linear(128, 64),
        #     nn.ReLU()
        # )
        
        # OPTIONAL: Ray Perception Encoder (using CNN 1D)
        # Uncomment below if you want to try CNN instead of linear encoder
        self.ray_encoder = nn.Sequential(
            nn.Conv1d(1, 16, kernel_size=3, padding=1),
            nn.ReLU(),
            nn.Conv1d(16, 32, kernel_size=3, padding=1),
            nn.ReLU(),
            nn.Flatten(),
            nn.Linear(32 * ray_obs_size, 64),
            nn.ReLU()
        )
        
        # Vector Sensor Encoder
        self.vec_encoder = nn.Sequential(
            nn.Linear(vec_obs_size, 64),
            nn.ReLU()
        )
        
        # Combined head
        self.combined = nn.Sequential(
            nn.Linear(64 + 64, 128),
            nn.ReLU(),
            nn.Linear(128, n_actions)
        )

    def forward(self, ray_obs, vec_obs):
        # If using CNN encoder, reshape ray_obs accordingly
        ray_obs = ray_obs.unsqueeze(1)  # Shape: [batch_size, 1, ray_obs_size]
        
        ray_out = self.ray_encoder(ray_obs)
        vec_out = self.vec_encoder(vec_obs)
        combined = torch.cat([ray_out, vec_out], dim=1)
        return self.combined(combined)

# DQN agent
class DQNAgent:
    def __init__(self, ray_obs_size, vec_obs_size, n_actions):
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.model = DQN(ray_obs_size, vec_obs_size, n_actions).to(self.device)
        self.target_model = DQN(ray_obs_size, vec_obs_size, n_actions).to(self.device)
        self.target_model.load_state_dict(self.model.state_dict())
        self.optimizer = optim.Adam(self.model.parameters(), lr=1e-3)
        self.memory = deque(maxlen=50000)
        self.batch_size = 64
        self.gamma = 0.99
        self.epsilon = 1.0
        self.epsilon_min = 0.05
        self.epsilon_decay = 0.995
        self.n_actions = n_actions
        self.ray_obs_size = ray_obs_size
        self.vec_obs_size = vec_obs_size

    def act(self, state):
        if random.random() < self.epsilon:
            return random.randint(0, self.n_actions - 1)
        
        state = torch.FloatTensor(state).unsqueeze(0).to(self.device)  # [1, 62]
        ray_obs = state[:, :self.ray_obs_size]
        vec_obs = state[:, self.ray_obs_size:]
        q_values = self.model(ray_obs, vec_obs)
        return torch.argmax(q_values).item()

    def remember(self, transition):
        self.memory.append(transition)

    def train_step(self):
        if len(self.memory) < self.batch_size:
            return

        batch = random.sample(self.memory, self.batch_size)
        states, actions, rewards, next_states, dones = zip(*batch)

        states = torch.FloatTensor(states).to(self.device)
        next_states = torch.FloatTensor(next_states).to(self.device)
        actions = torch.LongTensor(actions).unsqueeze(1).to(self.device)
        rewards = torch.FloatTensor(rewards).to(self.device)
        dones = torch.FloatTensor(dones).to(self.device)

        ray_states = states[:, :self.ray_obs_size]
        vec_states = states[:, self.ray_obs_size:]

        ray_next_states = next_states[:, :self.ray_obs_size]
        vec_next_states = next_states[:, self.ray_obs_size:]

        q_values = self.model(ray_states, vec_states).gather(1, actions).squeeze()
        next_q_values = self.target_model(ray_next_states, vec_next_states).max(1)[0]
        expected_q = rewards + (1 - dones) * self.gamma * next_q_values

        loss = nn.functional.mse_loss(q_values, expected_q)

        self.optimizer.zero_grad()
        loss.backward()
        self.optimizer.step()

    def update_target(self):
        self.target_model.load_state_dict(self.model.state_dict())

# Training loop
def train_dqn(env_path):
    # Connect to Unity
    channel = EngineConfigurationChannel()
    env = UnityEnvironment(file_name=env_path, side_channels=[channel])
    channel.set_configuration_parameters(time_scale=20.0)
    env.reset()

    behavior_name = list(env.behavior_specs.keys())[0]
    spec = env.behavior_specs[behavior_name]

    ray_obs_size = spec.observation_specs[0].shape[0]  # 55
    vec_obs_size = spec.observation_specs[1].shape[0]  # 7
    obs_size = ray_obs_size + vec_obs_size

    n_actions_move = spec.action_spec.discrete_branches[0]  # 5 moves
    n_actions_jump = spec.action_spec.discrete_branches[1]  # 2 jump choices
    n_actions = n_actions_move * n_actions_jump

    agent = DQNAgent(ray_obs_size, vec_obs_size, n_actions)

    episodes = 500
    max_steps = 1000
    target_update_interval = 10

    for episode in range(episodes):
        env.reset()
        decision_steps, terminal_steps = env.get_steps(behavior_name)
        idx = list(decision_steps)[0]

        ray_obs = decision_steps[idx].obs[0]
        vec_obs = decision_steps[idx].obs[1]
        obs = np.concatenate([ray_obs, vec_obs])

        total_reward = 0

        for step in range(max_steps):
            action = agent.act(obs)

            action_move = action // 2
            action_jump = action % 2

            action_tuple = spec.action_spec.empty_action(len(decision_steps))
            action_tuple.add_discrete(np.array([[action_move, action_jump]]))
            env.set_actions(behavior_name, action_tuple)
            env.step()

            decision_steps, terminal_steps = env.get_steps(behavior_name)

            if idx in terminal_steps:
                next_ray_obs = terminal_steps[idx].obs[0]
                next_vec_obs = terminal_steps[idx].obs[1]
                next_obs = np.concatenate([next_ray_obs, next_vec_obs])
                reward = terminal_steps[idx].reward
                done = True
            else:
                next_ray_obs = decision_steps[idx].obs[0]
                next_vec_obs = decision_steps[idx].obs[1]
                next_obs = np.concatenate([next_ray_obs, next_vec_obs])
                reward = decision_steps[idx].reward
                done = False

            agent.remember((obs, action, reward, next_obs, done))
            agent.train_step()

            obs = next_obs
            total_reward += reward

            if done:
                break

        if episode % target_update_interval == 0:
            agent.update_target()

        agent.epsilon = max(agent.epsilon_min, agent.epsilon * agent.epsilon_decay)
        print(f"Episode {episode}: Reward {total_reward}")

    env.close()

if __name__ == "__main__":
    train_dqn(env_path="RLEnvironment.app")
