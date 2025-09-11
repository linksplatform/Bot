# -*- coding: utf-8 -*-
"""Code verifier module for executing and validating Python code."""

import sys
import traceback
from io import StringIO
from contextlib import redirect_stdout, redirect_stderr
from typing import Tuple, Optional, Dict, Any


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
    
    def verify_and_fix_code(self, code: str, fix_callback=None) -> Tuple[bool, str, int]:
        """Verify code and attempt to fix it using a callback function.
        
        :param code: Initial Python code to verify
        :param fix_callback: Function that takes (code, error) and returns fixed code
        :return: Tuple of (final_success, final_output_or_code, attempts_used)
        """
        attempts = 0
        current_code = code
        
        while attempts < self.max_attempts:
            attempts += 1
            success, output, exception = self.execute_code(current_code)
            
            if success:
                return True, output, attempts
            
            # If no fix callback provided, return the error
            if not fix_callback:
                return False, output, attempts
            
            # Try to fix the code
            try:
                fixed_code = fix_callback(current_code, output)
                if fixed_code and fixed_code != current_code:
                    current_code = fixed_code
                else:
                    # No fix provided or same code returned
                    return False, output, attempts
            except Exception as fix_error:
                return False, f"Fix callback failed: {str(fix_error)}\nOriginal error: {output}", attempts
        
        return False, f"Max attempts ({self.max_attempts}) reached. Last error: {output}", attempts
    
    def reset_environment(self):
        """Reset the execution environment."""
        self.globals_dict.clear()
        self.locals_dict.clear()
    
    def get_environment_state(self) -> Dict[str, Any]:
        """Get current state of execution environment."""
        return {
            'globals': dict(self.globals_dict),
            'locals': dict(self.locals_dict)
        }
    
    def set_global_variable(self, name: str, value: Any):
        """Set a global variable in the execution environment."""
        self.globals_dict[name] = value
    
    def get_global_variable(self, name: str, default=None) -> Any:
        """Get a global variable from the execution environment."""
        return self.globals_dict.get(name, default)