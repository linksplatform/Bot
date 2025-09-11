#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Simple test for code verifier without dependencies."""

import sys
import traceback
from io import StringIO
from contextlib import redirect_stdout, redirect_stderr
from typing import Tuple, Optional, Dict, Any

# Copy of CodeVerifier for testing without module dependencies
class CodeVerifier:
    """Provides safe code execution and verification capabilities."""
    
    def __init__(self, max_attempts: int = 7):
        """Initialize code verifier.
        
        :param max_attempts: Maximum number of retry attempts for code correction
        """
        self.max_attempts = max_attempts
        self.globals_dict = {}
        self.locals_dict = {}
    
    def execute_code(self, code: str) -> Tuple[bool, str, Optional[Exception]]:
        """Execute Python code and capture output.
        
        :param code: Python code string to execute
        :return: Tuple of (success, output/error_message, exception)
        """
        # Capture stdout and stderr
        stdout_buffer = StringIO()
        stderr_buffer = StringIO()
        
        try:
            with redirect_stdout(stdout_buffer), redirect_stderr(stderr_buffer):
                # Execute the code in a controlled environment
                exec(code, self.globals_dict, self.locals_dict)
            
            # Get captured output
            stdout_output = stdout_buffer.getvalue()
            stderr_output = stderr_buffer.getvalue()
            
            # Combine outputs
            output = stdout_output
            if stderr_output:
                output += "\nErrors/Warnings:\n" + stderr_output
            
            return True, output.strip() if output.strip() else "Code executed successfully (no output)", None
            
        except Exception as e:
            # Get the full traceback
            exc_type, exc_value, exc_traceback = sys.exc_info()
            error_details = traceback.format_exception(exc_type, exc_value, exc_traceback)
            error_message = ''.join(error_details)
            
            return False, error_message, e
    
    def reset_environment(self):
        """Reset the execution environment."""
        self.globals_dict.clear()
        self.locals_dict.clear()

def test_code_verifier():
    """Test basic code verifier functionality."""
    print("Testing CodeVerifier...")
    
    # Initialize verifier
    verifier = CodeVerifier()
    
    # Test 1: Simple successful code
    print("\n1. Testing simple successful code:")
    code = "print('Hello, World!')"
    success, output, exception = verifier.execute_code(code)
    print(f"Success: {success}")
    print(f"Output: {output}")
    print(f"Exception: {exception}")
    assert success is True
    assert "Hello, World!" in output
    
    # Test 2: Code with variables
    print("\n2. Testing code with variables:")
    code = """
x = 10
y = 20
result = x + y
print(f"Sum: {result}")
"""
    success, output, exception = verifier.execute_code(code)
    print(f"Success: {success}")
    print(f"Output: {output}")
    assert success is True
    assert "Sum: 30" in output
    
    # Test 3: Code with error
    print("\n3. Testing code with error:")
    code = "print(undefined_variable)"
    success, output, exception = verifier.execute_code(code)
    print(f"Success: {success}")
    print(f"Output snippet: {output[:200]}...")  # Truncate for readability
    print(f"Exception type: {type(exception).__name__}")
    assert success is False
    assert "NameError" in output
    
    # Test 4: Environment persistence
    print("\n4. Testing environment persistence:")
    code1 = "persistent_var = 'I persist!'"
    success1, output1, _ = verifier.execute_code(code1)
    print(f"Set variable - Success: {success1}")
    
    code2 = "print(f'Variable: {persistent_var}')"
    success2, output2, _ = verifier.execute_code(code2)
    print(f"Use variable - Success: {success2}, Output: {output2}")
    assert success2 is True
    assert "I persist!" in output2
    
    # Test 5: Reset environment
    print("\n5. Testing environment reset:")
    verifier.reset_environment()
    code3 = "print(persistent_var)"
    success3, output3, _ = verifier.execute_code(code3)
    print(f"After reset - Success: {success3}")
    assert success3 is False
    assert "NameError" in output3
    
    print("\n✅ All core tests passed!")

if __name__ == "__main__":
    test_code_verifier()