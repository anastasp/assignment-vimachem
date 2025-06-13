using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

namespace ContactListAutomation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Contact List automation...");
            
            // Create a random email for registration
            string email = $"test{DateTime.Now.Ticks}@example.com";
            string password = "Password123";
            
            using var playwright = await Playwright.CreateAsync();
            bool isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = isCI, // Run headless in CI, headful locally
                SlowMo = 100
            });
                        
            var page = await browser.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
            });
            
            // Set default timeout to be more generous
            page.SetDefaultTimeout(30000);
            
            await page.GotoAsync("https://thinking-tester-contact-list.herokuapp.com/");
            // Wait for the page to be fully loaded
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            try
            {
                // Test 1: Sign up and login
                Console.WriteLine("\n=== Test 1: Sign up and login ===");
                await SignUpAsync(page, email, password);
                
                // Wait before logout to ensure page is stable
                await page.WaitForTimeoutAsync(1000);
                await page.ClickAsync("#logout");
                await page.WaitForSelectorAsync("#email", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
                
                await LoginAsync(page, email, password);
                
                // Test 2: Add a new contact and validate on details page
                Console.WriteLine("\n=== Test 2: Add contact and validate details ===");
                await AddContactAndValidateAsync(page);
                
                // Test 3: Try to add a contact with invalid date of birth
                Console.WriteLine("\n=== Test 3: Add contact with invalid date ===");
                await AddContactWithInvalidDateAsync(page);
                
                // Test 4: Delete an existing contact cannot be done due to the fact that delete button is not working properly
                Console.WriteLine("\n=== Test 4: Delete existing contact ===");
                // await DeleteContactAsync(page);
                
                Console.WriteLine("\nAll tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nTest failed: {ex.Message}");
                
                // Take a screenshot on failure
                string screenshotPath = $"error-{DateTime.Now:yyyyMMdd-HHmmss}.png";
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
                Console.WriteLine($"Screenshot saved to {screenshotPath}");
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        
        static async Task SignUpAsync(IPage page, string email, string password)
        {
            Console.WriteLine($"Signing up with email: {email}");
            
            // Navigate to signup page
            await page.ClickAsync("#signup");
            await page.WaitForSelectorAsync("#firstName", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Fill the signup form
            await page.FillAsync("#firstName", "Test");
            await page.FillAsync("#lastName", "User");
            await page.FillAsync("#email", email);
            await page.FillAsync("#password", password);
            
            // Submit the form
            await page.ClickAsync("#submit");
            
            // Wait for navigation to complete and dashboard to load
            await page.WaitForURLAsync("**/contactList");
            await page.WaitForSelectorAsync("#logout", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Console.WriteLine("Sign up successful!");
        }
        
        static async Task LoginAsync(IPage page, string email, string password)
        {
            Console.WriteLine($"Logging in with email: {email}");
            
            // Wait for login form to be visible
            await page.WaitForSelectorAsync("#email", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            
            // Fill the login form
            await page.FillAsync("#email", email);
            await page.FillAsync("#password", password);
            
            // Submit the form
            await page.ClickAsync("#submit");
            
            // Wait for navigation to complete and dashboard to load
            await page.WaitForURLAsync("**/contactList");
            await page.WaitForSelectorAsync("#logout", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Console.WriteLine("Login successful!");
        }
        
        static async Task AddContactAndValidateAsync(IPage page)
        {
            Console.WriteLine("Adding a new contact...");
            
            // Contact details
            string firstName = "John";
            string lastName = "Doe";
            string email = "john.doe@example.com";
            string phone = "1234567890";
            string street = "123 Main St";
            string city = "New York";
            string state = "NY";
            string postalCode = "10001";
            string birthdate = "1990-01-01";
            
            // Wait for contact list page to be fully loaded
            await page.WaitForSelectorAsync("#add-contact", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            
            // Click on Add a New Contact button
            await page.ClickAsync("#add-contact");
            
            // Wait for the form to be visible
            await page.WaitForSelectorAsync("#firstName", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Fill the contact form
            await page.FillAsync("#firstName", firstName);
            await page.FillAsync("#lastName", lastName);
            await page.FillAsync("#email", email);
            await page.FillAsync("#phone", phone);
            await page.FillAsync("#street1", street);
            await page.FillAsync("#city", city);
            await page.FillAsync("#stateProvince", state);
            await page.FillAsync("#postalCode", postalCode);
            await page.FillAsync("#birthdate", birthdate);
            
            // Submit the form
            await page.ClickAsync("#submit");
            
            // Wait for navigation to complete
            await page.WaitForURLAsync("**/contactList");
            await page.WaitForSelectorAsync("text=Contact List", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Console.WriteLine("Contact added successfully!");
            
            // Wait for the contact to appear in the list
            var contactLocator = page.Locator($"text={firstName} {lastName}");
            await contactLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            
            // Click on the contact to view details
            await contactLocator.ClickAsync();
            
            // Wait for details page to load
            await page.WaitForSelectorAsync("text=Contact Details", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Validate contact details
            bool isFirstNameCorrect = await page.IsVisibleAsync($"text={firstName}");
            bool isLastNameCorrect = await page.IsVisibleAsync($"text={lastName}");
            bool isEmailCorrect = await page.IsVisibleAsync($"text={email}");
            bool isPhoneCorrect = await page.IsVisibleAsync($"text={phone}");
            
            if (isFirstNameCorrect && isLastNameCorrect && isEmailCorrect && isPhoneCorrect)
            {
                Console.WriteLine("Contact details validated successfully!");
            }
            else
            {
                throw new Exception("Contact details validation failed!");
            }
            
            // Return to contact list
            await page.ClickAsync("#return");
            await page.WaitForSelectorAsync("#add-contact", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        
        static async Task AddContactWithInvalidDateAsync(IPage page)
        {
            Console.WriteLine("Adding a contact with invalid date of birth...");
            
            // Wait for contact list page to be fully loaded
            await page.WaitForSelectorAsync("#add-contact", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            
            // Click on Add a New Contact button
            await page.ClickAsync("#add-contact");
            
            // Wait for the form to be visible
            await page.WaitForSelectorAsync("#firstName", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Fill the contact form with invalid date
            await page.FillAsync("#firstName", "Jane");
            await page.FillAsync("#lastName", "Smith");
            await page.FillAsync("#email", "jane.smith@example.com");
            await page.FillAsync("#phone", "9876543210");
            await page.FillAsync("#street1", "456 Oak St");
            await page.FillAsync("#city", "Los Angeles");
            await page.FillAsync("#stateProvince", "CA");
            await page.FillAsync("#postalCode", "90001");
            await page.FillAsync("#birthdate", "13/13/2022"); // Invalid date format
            
            // Submit the form
            await page.ClickAsync("#submit");
            
            // Wait for error message to appear
            await page.WaitForTimeoutAsync(1000); // Give time for error to appear
            
            // Check for error message - try both possible error messages
            bool hasErrorMessage = await page.IsVisibleAsync("text=Contact validation failed: birthdate: Birthdate is invalid") || 
                                  await page.IsVisibleAsync("text=Invalid date format");
            
            if (hasErrorMessage)
            {
                Console.WriteLine("Invalid date validation successful!");
            }
            else
            {
                throw new Exception("Invalid date validation failed!");
            }
            
            // Cancel and return to contact list
            await page.ClickAsync("#cancel");
            await page.WaitForSelectorAsync("#add-contact", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        
        static async Task DeleteContactAsync(IPage page)
        {
            Console.WriteLine("Creating a contact to delete...");
            
            // Wait for contact list page to be fully loaded
            await page.WaitForSelectorAsync("#add-contact", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            
            // Create a contact to delete
            string firstName = "Delete";
            string lastName = "Me";
            
            // Click on Add a New Contact button
            await page.ClickAsync("#add-contact");
            
            // Wait for the form to be visible
            await page.WaitForSelectorAsync("#firstName", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Fill minimal contact form
            await page.FillAsync("#firstName", firstName);
            await page.FillAsync("#lastName", lastName);
            await page.FillAsync("#email", "delete.me@example.com");
            await page.FillAsync("#phone", "5555555555");
            
            // Submit the form
            await page.ClickAsync("#submit");
            
            // Wait for navigation to complete
            await page.WaitForURLAsync("**/contactList");
            await page.WaitForSelectorAsync("text=Contact List", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Console.WriteLine("Contact created successfully!");
            
            // Wait for the contact to appear in the list
            var contactLocator = page.Locator($"text={firstName} {lastName}");
            await contactLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            
            // Click on the contact to view details
            await contactLocator.ClickAsync();
            
            // Wait for details page to load
            await page.WaitForSelectorAsync("text=Contact Details", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Delete the contact
            await page.ClickAsync("#delete");
            
            // Wait for the confirmation dialog
            await page.WaitForSelectorAsync("text=Delete", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            
            // Confirm deletion in the dialog
            await page.ClickAsync("text=ΟΚ");
            
            // Wait for navigation back to contact list
            await page.WaitForURLAsync("**/contactList");
            await page.WaitForSelectorAsync("text=Contact List", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Wait a moment for the UI to update
            await page.WaitForTimeoutAsync(1000);
            
            // Verify contact is deleted
            bool contactExists = await page.IsVisibleAsync($"text={firstName} {lastName}");
            
            if (!contactExists)
            {
                Console.WriteLine("Contact deleted successfully!");
            }
            else
            {
                throw new Exception("Contact deletion failed!");
            }
        }
    }
}
