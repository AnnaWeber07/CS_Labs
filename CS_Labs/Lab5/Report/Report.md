﻿# Lab work 3: Asymmetric Ciphers.

### Course: Cryptography & Security
### Author: Anna Chiriciuc

---

## Theory

Authentication & authorization are 2 of the main security goals of IT systems and should not be used interchangibly. Simply put, during authentication the system verifies the identity of a user or service, and during authorization the system checks the access rights, optionally based on a given user role.

In this lab work I've implemented an MFA algorithm, a pretty basic and simple one - a question to verify the author identity (similar to how ATM machines work - your debit card is one authentication factor, whereas PIN is an another method).

As of front end part, I used Swagger UI.

## Objectives:

1. I've based my work on previous lab work number 4 that contained a hashing example of user passwords and messages (SHA256 + Salt) and JWT tokens. 
2. Basic authentication and MFA are implemented.
3. The simulation is provided as in the 4th lab work - authentication, registration, login, tokens of access.


## Implementation description

In this lab work I've created a web application that has in-built database of users.
These operations are available: register a user, login, get the message to be encrypted, verify it and create refresh tokens + the MFA (a question for the user).

In order to omit lots of code, I'll explain the structure. I have implemented necessary controllers for the requests mentioned earlier, DTOs with necessary properties for json object creation/deserialization and specific methods for password and message hashing.

JWTs (JSON Web Tokens) are credentials, which can grant access to resources.
I used JWTs in order to transfer the data for authentication purposes in this client-server app.
They are created at the server's side, then signed with a private key and transmitted over to the client's side, which lated uses it for user validation.

Also I've implemented MFA here.

Some code snippets here:

Registration
``` 
  [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);
            CreateMessageHash(request.Answer, out byte[] answerHash, out byte[] answerSalt);

            user.Username = request.Username;
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.Question = request.Question;
            user.AnswerHash = answerHash;
            user.AnswerSalt = answerSalt;

            return Ok("You are registered");
        }
``` 

Login
``` 
 [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDto request)
        {
            if (user.Username != request.Username)
            {
                return BadRequest("User not found.");
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest("Wrong password.");
            }
            if (!VerifyAnswerHash(request.Answer, user.AnswerHash, user.AnswerSalt))
            {
                return BadRequest("Wrong Answer");
            }
            

            string token = CreateToken(user);

            var refreshToken = GenerateRefreshToken();
            CreateUpdateToken(refreshToken);
            user.RefreshToken = token;

            return Ok(token);
        }
``` 


Refresh JWT token
``` 
 [HttpPost("refresh-token")]
        public async Task<ActionResult<string>> UpdateToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (!user.RefreshToken.Equals(refreshToken))
            {
                return Unauthorized("Invalid Refresh Token.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Unauthorized("Token expired.");
            }

            string token = CreateToken(user);
            var newRefreshToken = GenerateRefreshToken();
            CreateUpdateToken(newRefreshToken);

            return Ok(token);
        }
``` 

In order to omit a lot of code, message hash, salt, etc will be omitted, but can be found in AuthController.cs file. :)
Link: https://github.com/AnnaWeber07/CS_Labs/blob/master/CS_Labs/Lab5/Controllers/AuthController.cs


## Conclusion

As a result we obtain a whole secure web app that is based on MFA, Salt+SHA256, JWT tokens and complete authorization/registration.
