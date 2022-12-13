# QA40x_BareMetal-Private
This repo shows how to to use USB writes to control the QA40x hardware. There are four ways of controlling the QA40x hardware:

**QA40x Application**: This allows you to make measurements in a self-contained environment. 

**QA40x REST**: This allows you to control the QA40x hardware via the QA40x application. The control occurs via REST calls. This requires proficiency is writing software.

**Tractor**: Tractor is a standalone application that controls the QA40x application via REST. The application allows you to quickly build test scripts, specify pass/fail windows for measurement results and permit logging to databases. The scripts can also include operator instructions to allow manual testing by playing a WAV file (for example). This allows factory personnel to verify if pots are scratchy, for example. Tractor doesn't require any coding for standard tests.

**Bare Metal**: The Bare Metal inter
