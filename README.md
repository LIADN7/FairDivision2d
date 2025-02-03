# Fair Division 2D

<img src="https://github.com/LIADN7/FairDivision2d/blob/main/Assets/Imgs/VisualImages/SubjectiveDivisions.png" width="400" height="400"> <img src="https://github.com/LIADN7/FairDivision2d/blob/main/Assets/Imgs/VisualImages/CutMap.jpg" width="203" height="512">

## Introduction

FairDivision2D is a research-driven program exploring fair division algorithms in two dimensions, forming part of a Master's thesis in Computer Science at the Open University. The project focuses on:

* Comparing structured fair-division algorithms with negotiation-based methods.

* Evaluating the impact of private vs. public valuations.

* Analyzing communication’s role in fair division.

## Implemented Algorithms

1. Symmetric Cut and Choose (2 players): Players independently mark a "halfway" cut. If cuts overlap, a predefined selection process resolves the division.

2. The Last Diminisher (3 players): Players sequentially adjust a portion of land to match their valuation; the last to adjust keeps the portion.

2. Round Robin (2 players): Players take turns selecting perceived equal-value land sections until all land is distributed.

## Tools Used
The following tools were utilized in this project:

1. [**Unity**](https://unity.com/) Game engine for visualization.

2. [**Photon Unity Networking (PUN2)**](https://www.photonengine.com/pun) Enables real-time multiplayer interaction.

3. [**Unity Cloud Database**](https://cloud.unity.com/home/) Stores player data and results.


## Experiment Setup
Participants engage in fair division tasks under different conditions:

* Private Valuations: Each player knows only their own valuation.

* Algorithm-Guided Division: Players receive algorithm suggestions.

* Algorithm + Communication: Players use both algorithm guidance and chat.


## Experiment Workflow

1. Practice Round: Players practice with AI opponents.

2. Real Rounds:

    * Rounds 1-2: No valuation visibility, no communication.

    * Rounds 3-4: Randomly assigned to either valuation heat maps or chat communication.

    * Rounds 5-6: Both valuation visibility and chat communication.


## Data Collection & Analysis

* Collected data includes:

* Player decisions and timing.

* Chat logs for negotiation analysis.

* Heat map interactions.

* Fairness metrics (social welfare, envy, satisfaction levels).

* Geometric analysis of allocated land.


## Future Enhancements

* Improved heat map visualization.

* Enhanced UI for valuation insights.

* Automated statistical analysis.

* Additional experiments with binding agreements.


### Additional Resources

For more details, visit: **[Fair Division 2D Research Website](https://sites.google.com/view/landdivision2d/home?authuser=0)**

---

### Credits

This research is guided by:

* Prof. Rica Gonen

* Prof. Erel Segal-Halevi

* Prof. Josue Ortega




