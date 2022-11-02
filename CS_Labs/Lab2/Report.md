# Lab work 2: Symmetric Ciphers. Stream Ciphers. Block Ciphers.

### Course: Cryptography & Security
### Author: Anna Chiriciuc

---

## Theory

Symmetric cryptography encrypts plain text while having only 1 private key. There are two types of cipher: Stream and Block Ciphers.

Here I've used Twofish and Rabbit.

Rabbit is a high-speed stream cipher, whilst Twofish is a block cipher.

**Rabbit** - It takes a 128- bit secret key and a 64-bit IV (if desired) as input and generates for each iteration an output block of 128 pseudo-random bits from a combination of the internal state bits.


**Twofish** - descendor of blowfish. Twofish is one of the most secure encryption protocols. In theory, its high block size means that Twofish is safe from brute-force attacks, since such an attack would require a tremendous amount of processing power to decrypt a 128-bit encrypted message.

## Objectives:

I've got familiar with this type of ciphers and I've implemented them. See AnnaWeber07/CS_Labs/Lab2 on Github. Tests are attached there.

## Implementation description


Since the code is really bulky and big, I've put here the algorithms behind Rabbit and Twofish.

**Rabbit** - the internal state of this stream cipher consists of 513 bits. 512 bits are divided between eight 32-bit state variables x and eigth 32-bit counter variables c. The counter carry bit theta, which needs to be stored between iterations, is initialized to zero. The eight state variables and the eight counters are derived from the key at initialization.

We setup the key by expanding the 128-bit key into both eight state and counter variables so that we have a  one-to-one correspondence.
Then the system iterates this four times, according to the next-state function, to diminish correlations between bits in the key in internal state variables.
Then we add to x's additions (modulo 2^32). The bits are XORed.


**Twofish** - in each round, two 32-bit words act as inputs into the F function. Each word is broken up into 4 bytes, which are then sent through four key-dependent S-boxes (8-bit I/O). Then the maximum distance separable matrix combines the 4 output bytes into a 32-bit word. These words are combined using a PHT, and lastly they're added to two round subkeys and a XOR function to the right is performed.

rabb

## Conclusions / Screenshots / Results
As a result we obtain this:

Rabbit:
![image](https://user-images.githubusercontent.com/78998404/197786174-435285a1-a5fe-459c-ab1b-9bb60b75c326.png)


Twofish:
![image](https://user-images.githubusercontent.com/78998404/197785984-42511df6-0a44-4894-a38f-5993f7fc9d54.png)

