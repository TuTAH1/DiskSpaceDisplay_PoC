This work serves as a proof of concept, showcasing my vision of the most informative way to present disk information.

# Details

The idea is simple — divide the occupied/free space bar into blocks of a fixed byte size (e.g., 100 GB). This way, both used and remaining space are immediately clear at a glance. Unlike my approach, the "classic" solid bar representation provides no absolute information about how much space is used or free; it only shows a relative percentage, which is of little practical use.

## Solid bar (classical widespread solution)
![Скриншот 04-02-2025 200837](https://github.com/user-attachments/assets/c00202e0-2837-4d9a-bb2d-690576972c49)
* Total drive space can be known only by looking on the numbers
* Remaining space shown in percent from total drive space, it's not easy to estimate absolute remaining/occupied space (how much GB left)

## Bar with fixed byte size blocks (my proposed solution)
![Скриншот 04-02-2025 200909](https://github.com/user-attachments/assets/011d67a9-eb3e-4945-bd67-9c415ba467ee)
* Total drive space can be easily visually estimated (only if it's larger than block size).
* Remaining space still shown in percent¹ and it's easy to estimate absolute remaining/occupied space (how much GB left) visually.

  ¹ A bit inaccurate, becouse I didn't deal with spacings in partial block fill calculations, but it's not the idea's problem
